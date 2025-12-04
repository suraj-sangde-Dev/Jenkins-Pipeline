using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Linq;
using TeleICU.API.DTOs.CaseDto;
using TeleICU.API.Repository.Interface;

namespace TeleICU.API.Repository
{
    public class CaseRepository : ICaseRepository
    {
        private readonly DapperContext _context;

        public CaseRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<int> CreateCaseAsync(CreateCaseDto request, int createdBy)
        {
            using var connection = _context.CreateConnection();

            // If CaseId provided, update that case
            if (request.CaseId.HasValue && request.CaseId.Value > 0)
            {
                var rows = await connection.ExecuteAsync(@"
                    UPDATE Cases 
                    SET ChiefComplaint = @ChiefComplaint,
                        ComplaintDescription = @ComplaintDescription,
                        UpdatedDate = NOW()
                    WHERE CaseId = @CaseId",
                    new
                    {
                        CaseId = request.CaseId.Value,
                        request.ChiefComplaint,
                        request.ComplaintDescription
                    });
                return rows > 0 ? request.CaseId.Value : 0;
            }

            // Otherwise, try to reuse the latest case for this patient
            var existingCaseId = await connection.ExecuteScalarAsync<int?>(
                "SELECT CaseId FROM Cases WHERE PatientId = @PatientId ORDER BY CreatedDate DESC LIMIT 1",
                new { request.PatientId });

            if (existingCaseId.HasValue && existingCaseId.Value > 0)
            {
                var rows = await connection.ExecuteAsync(@"
                    UPDATE Cases 
                    SET ChiefComplaint = @ChiefComplaint,
                        ComplaintDescription = @ComplaintDescription,
                        UpdatedDate = NOW()
                    WHERE CaseId = @CaseId",
                    new
                    {
                        CaseId = existingCaseId.Value,
                        request.ChiefComplaint,
                        request.ComplaintDescription
                    });
                return rows > 0 ? existingCaseId.Value : 0;
            }

            // No existing case -> create new
            return await connection.ExecuteScalarAsync<int>(@"
                INSERT INTO Cases (PatientId, CreatedBy, ChiefComplaint, ComplaintDescription)
                VALUES (@PatientId, @CreatedBy, @ChiefComplaint, @ComplaintDescription);
                SELECT LAST_INSERT_ID();",
                new
                {
                    request.PatientId,
                    CreatedBy = createdBy,
                    request.ChiefComplaint,
                    request.ComplaintDescription
                });
        }

        public async Task AddHistoryAsync(CaseHistoryDto request)
        {
            using var connection = _context.CreateConnection();

            // Treat as upsert: replace existing history for this case
            await connection.ExecuteAsync("DELETE FROM CaseHistory WHERE CaseId = @CaseId", new { request.CaseId });

            List<(string Type, string Name)> items = new();

            void AddIfPresent(string? value, string type)
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                items.Add((type, value.Trim()));
            }

            AddIfPresent(request.Medical, "Medical");
            AddIfPresent(request.Family, "Family");
            AddIfPresent(request.Allergies, "Allergy");

            if (items.Count == 0) return;

            var sql = @"INSERT INTO CaseHistory (CaseId, HistoryType, ConditionName)
                        VALUES (@CaseId, @HistoryType, @ConditionName)";

            await connection.ExecuteAsync(sql, items.Select(i => new { CaseId = request.CaseId, HistoryType = i.Type, ConditionName = i.Name }));
        }


        public async Task AddMedicationsAsync(IEnumerable<PreAdmitMedicationDto> requests)
        {
            if (requests == null) return;
            var valid = requests.Where(r => r != null).ToList();
            if (valid.Count == 0) return;

            // Treat as upsert: replace existing medications for the case(s)
            using var connection = _context.CreateConnection();
            var caseIds = valid.Select(r => r.CaseId).Distinct().ToList();
            foreach (var cid in caseIds)
            {
                await connection.ExecuteAsync("DELETE FROM CasePreAdmitMedications WHERE CaseId = @CaseId", new { CaseId = cid });
            }

            // Generate ConsultationId for items where it's not provided
            // Build daily prefix from DB clock to keep consistency with MySQL timestamps
            var prefix = await connection.ExecuteScalarAsync<string>("SELECT DATE_FORMAT(NOW(), 'CONS/%Y/%m/%d')");
            // Get current max numeric suffix for today
            var currentMax = await connection.ExecuteScalarAsync<int>(
                @"SELECT COALESCE(MAX(CAST(SUBSTRING_INDEX(ConsultationId,'/',-1) AS UNSIGNED)),0)
                  FROM CasePreAdmitMedications
                  WHERE ConsultationId LIKE CONCAT(@Prefix,'/%')",
                new { Prefix = prefix });

            int next = currentMax; // will pre-increment below
            foreach (var r in valid)
            {
                // Always auto-generate; ignore any provided value
                next += 1;
                r.ConsultationId = $"{prefix}/{next:D4}";
            }

            var items = valid.Select(r => new
            {
                r.CaseId,
                r.Medicine,
                r.Frequency,
                r.Dose,
                r.Type,
                r.DurationValue,
                r.DurationType,
                r.PrescribedBy,
                r.ConsultationId,
                r.Status
            }).ToList();
            if (items.Count == 0) return;
            var sql = @"INSERT INTO CasePreAdmitMedications 
                        (CaseId, Medicine, Frequency, Dose, Type, DurationValue, DurationType, PrescribedBy, ConsultationId, Status)
                        VALUES (@CaseId, @Medicine, @Frequency, @Dose, @Type, @DurationValue, @DurationType, @PrescribedBy, @ConsultationId, @Status)";
            await connection.ExecuteAsync(sql, items);
        }

        public async Task AddVitalAsync(VitalDto request)
        {
            using var connection = _context.CreateConnection();
            // Fetch PatientId for the given CaseId to satisfy NOT NULL constraint on Vitals.PatientId
            var patientId = await connection.ExecuteScalarAsync<int>(
                "SELECT PatientId FROM Cases WHERE CaseId = @CaseId",
                new { request.CaseId }
            );
            // Fetch DOB to compute age for age-adjusted thresholds
            var dob = await connection.ExecuteScalarAsync<DateTime?>(
                "SELECT DOB FROM Patients WHERE PatientId = @PatientId",
                new { PatientId = patientId });
            int ageYears = 0;
            if (dob.HasValue)
            {
                var today = DateTime.UtcNow.Date;
                ageYears = today.Year - dob.Value.Year - (today < dob.Value.Date.AddYears(today.Year - dob.Value.Year) ? 1 : 0);
                if (ageYears < 0) ageYears = 0;
            }

            // Compute patient's condition based on incoming vitals and age
            var condition = ComputeCondition(request, ageYears);

            // Detect if Vitals.Condition column exists
            var hasConditionColumn = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM information_schema.COLUMNS 
                  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Vitals' AND COLUMN_NAME = 'Condition'"
            ) > 0;

            // Optionally update Patients.CurrentCondition if exists
            var hasCurrentCondition = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) FROM information_schema.COLUMNS 
                  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Patients' AND COLUMN_NAME = 'CurrentCondition'"
            ) > 0;

            // Always insert a new vitals row to build trend history
            var insertSql = hasConditionColumn
                ? @"INSERT INTO Vitals 
                    (CaseId, PatientId, Temperature, RespiratoryRate, OxygenSaturation, BloodPressureDIA, BloodPressureSYS,
                     HeartRate, PulseRate, BloodGlucose, FIO2, ETCO2, BMI, RightAtrialPressure, `Condition`)
                    VALUES (@CaseId, @PatientId, @Temperature, @RespiratoryRate, @OxygenSaturation, @BloodPressureDIA, @BloodPressureSYS,
                            @HeartRate, @PulseRate, @BloodGlucose, @FIO2, @ETCO2, @BMI, @RightAtrialPressure, @Condition)"
                : @"INSERT INTO Vitals 
                    (CaseId, PatientId, Temperature, RespiratoryRate, OxygenSaturation, BloodPressureDIA, BloodPressureSYS,
                     HeartRate, PulseRate, BloodGlucose, FIO2, ETCO2, BMI, RightAtrialPressure)
                    VALUES (@CaseId, @PatientId, @Temperature, @RespiratoryRate, @OxygenSaturation, @BloodPressureDIA, @BloodPressureSYS,
                            @HeartRate, @PulseRate, @BloodGlucose, @FIO2, @ETCO2, @BMI, @RightAtrialPressure)";

            await connection.ExecuteAsync(insertSql,
                new
                {
                    request.CaseId,
                    PatientId = patientId,
                    request.Temperature,
                    request.RespiratoryRate,
                    request.OxygenSaturation,
                    request.BloodPressureDIA,
                    request.BloodPressureSYS,
                    request.HeartRate,
                    request.PulseRate,
                    request.BloodGlucose,
                    request.FIO2,
                    request.ETCO2,
                    request.BMI,
                    request.RightAtrialPressure,
                    Condition = condition
                });

            if (hasCurrentCondition)
            {
                await connection.ExecuteAsync(
                    "UPDATE Patients SET CurrentCondition = @Condition, UpdatedDate = NOW() WHERE PatientId = @PatientId",
                    new { Condition = condition, PatientId = patientId });
            }
        }
        
        private static string ComputeCondition(VitalDto v, int ageYears)
        {
            // Age-bracketed normal ranges (derived from your chart; simplified brackets)
            (int hrMin, int hrMax) = ageYears switch
            {
                < 1 => (90, 160),
                1 or 2 => (80, 120),
                >= 3 and <= 5 => (65, 100),
                >= 6 and <= 11 => (58, 90),
                >= 12 and <= 15 => (50, 90),
                _ => (60, 100) // adult default
            };

            (int rrMin, int rrMax) = ageYears switch
            {
                < 1 => (30, 53),
                1 or 2 => (22, 37),
                >= 3 and <= 5 => (20, 28),
                >= 6 and <= 11 => (18, 25),
                >= 12 and <= 15 => (12, 20),
                _ => (12, 20) // adult
            };

            // Systolic by age (approx from chart)
            (int sbpMin, int sbpMax) = ageYears switch
            {
                < 1 => (72, 104),
                1 or 2 => (86, 106),
                >= 3 and <= 5 => (89, 115),
                >= 6 and <= 11 => (97, 125),
                >= 12 and <= 15 => (110, 131),
                _ => (90, 140) // adult
            };

            // Diastolic by age (approx from chart)
            (int dbpMin, int dbpMax) = ageYears switch
            {
                < 1 => (37, 67),
                1 or 2 => (42, 76),
                >= 3 and <= 5 => (46, 72),
                >= 6 and <= 11 => (57, 81),
                >= 12 and <= 15 => (64, 83),
                _ => (60, 90) // adult
            };

            // Temperature (F)
            const decimal tempLowSevere = 100m; // fever
            const decimal tempLowCritical = 95m; // hypothermia
            const decimal tempHighCritical = 103m; // high fever

            // Oxygen saturation
            bool spo2Critical = v.OxygenSaturation < 90m;
            bool spo2Severe = v.OxygenSaturation >= 90m && v.OxygenSaturation < 94m;

            // Blood glucose (mg/dL)
            bool glucoseCritical = v.BloodGlucose < 50m || v.BloodGlucose > 300m;
            bool glucoseSevere = (v.BloodGlucose >= 50m && v.BloodGlucose < 70m) || (v.BloodGlucose > 200m && v.BloodGlucose <= 300m);

            // ETCO2 (mmHg)
            bool etco2Critical = v.ETCO2 < 20m || v.ETCO2 > 50m;
            bool etco2Severe = (v.ETCO2 >= 20m && v.ETCO2 < 25m) || (v.ETCO2 > 45m && v.ETCO2 <= 50m);

            // Right atrial pressure (mmHg)
            bool rapCritical = v.RightAtrialPressure < 0m || v.RightAtrialPressure > 20m;
            bool rapSevere = v.RightAtrialPressure > 7m && v.RightAtrialPressure <= 20m;

            // FIO2 burden combined with SpO2
            bool fio2Critical = v.FIO2 >= 80m && v.OxygenSaturation < 90m; // high oxygen requirement with hypoxia
            bool fio2Severe = (v.FIO2 >= 60m && v.OxygenSaturation < 92m);

            // BMI (very rough flags; pediatrics need z-scores but using broad guardrails)
            bool bmiCritical = v.BMI < 10m || v.BMI > 40m;
            bool bmiSevere = (v.BMI >= 10m && v.BMI < 13m) || (v.BMI > 35m && v.BMI <= 40m);

            // HR/RR/SBP/DBP critical: far outside normal
            bool hrCritical = v.HeartRate < Math.Max(30, hrMin - 20) || v.HeartRate > Math.Min(200, hrMax + 30);
            bool rrCritical = v.RespiratoryRate < Math.Max(5, rrMin - 10) || v.RespiratoryRate > rrMax + 10;
            bool sbpCritical = v.BloodPressureSYS < Math.Max(50, sbpMin - 20) || v.BloodPressureSYS > sbpMax + 40;
            bool dbpCritical = v.BloodPressureDIA < Math.Max(30, dbpMin - 15) || v.BloodPressureDIA > dbpMax + 25;
            bool tempCritical = v.Temperature < tempLowCritical || v.Temperature > tempHighCritical;

            if (spo2Critical || hrCritical || rrCritical || sbpCritical || dbpCritical || tempCritical || glucoseCritical || etco2Critical || rapCritical || fio2Critical || bmiCritical)
                return "Critical";

            // Severe: moderately outside normal ranges or concerning combinations
            bool hrSevere = v.HeartRate < hrMin || v.HeartRate > hrMax;
            bool rrSevere = v.RespiratoryRate < rrMin || v.RespiratoryRate > rrMax;
            bool sbpSevere = v.BloodPressureSYS < sbpMin || v.BloodPressureSYS > sbpMax;
            bool dbpSevere = v.BloodPressureDIA < dbpMin || v.BloodPressureDIA > dbpMax;
            bool tempSevere = v.Temperature >= tempLowSevere && v.Temperature <= tempHighCritical;
            bool pulseSevere = v.PulseRate < hrMin || v.PulseRate > hrMax; // align with HR thresholds

            if (spo2Severe || hrSevere || rrSevere || sbpSevere || dbpSevere || tempSevere || glucoseSevere || etco2Severe || rapSevere || fio2Severe || bmiSevere || pulseSevere)
                return "Severe";

            return "Stable";
        }

        public async Task AddHealthRecordAsync(int caseId, string recordType, string filePathUrl)
        {
            using var connection = _context.CreateConnection();

            await connection.ExecuteAsync(@"
        INSERT INTO CaseHealthRecords (CaseId, RecordType, FilePath)
        VALUES (@CaseId, @RecordType, @FilePath)",
                new { CaseId = caseId, RecordType = recordType, FilePath = filePathUrl });
        }


        public async Task AddCaseQueryAsync(CaseQueryDto dto)
        {
            using var connection = _context.CreateConnection();
            var sql = @"UPDATE Cases 
                SET Query = @Query, UpdatedDate = NOW() 
                WHERE CaseId = @CaseId";
            await connection.ExecuteAsync(sql, new { CaseId = dto.CaseId, dto.Query });
        }

        public async Task<PatientCaseViewDto?> GetCaseViewAsync(int caseId)
        {
            using var connection = _context.CreateConnection();

            // Header and case details
            var header = await connection.QueryFirstOrDefaultAsync<CaseViewHeaderRow>(@"SELECT 
                c.CaseId,
                p.PatientId,
                CONCAT(p.FirstName, ' ', COALESCE(p.LastName,'')) AS PatientName,
                CAST(TIMESTAMPDIFF(YEAR, p.DOB, CURDATE()) AS SIGNED) AS AgeYears,
                p.Gender,
                b.BedNumber,
                s.SpokeHospitalName AS ICU,
                p.AdmitDate AS AdmitDateTime,
                s.AddressLine1 AS SpokeAddressLine1,
                s.AddressLine2 AS SpokeAddressLine2,
                st.StateName AS SpokeState,
                d.DistrictName AS SpokeDistrict,
                ci.CityName AS SpokeCity,
                s.PIN AS SpokePIN,
                CONCAT(u.FirstName, ' ', COALESCE(u.LastName, '')) AS SpecialistName,
                u.SignaturePath,
                u.Speciality,
                c.ChiefComplaint,
                c.ComplaintDescription,
                c.Query,
                c.Examination,
                c.Advice,
                cpm.ConsultationId,
                v.UpdatedDate AS VitalUpdate,
                cpm.PrescribedBy

            FROM Cases c
            JOIN Patients p ON p.PatientId = c.PatientId
            LEFT JOIN Beds b ON b.BedId = p.BedId
            LEFT JOIN Spokes s ON s.SpokeId = p.SpokeId
            LEFT JOIN States st ON st.StateId = s.StateId
            LEFT JOIN Districts d ON d.DistrictCode = s.District
            LEFT JOIN Cities ci ON ci.CityCode = s.City
            LEFT JOIN CasePreAdmitMedications cpm ON cpm.CaseId = c.CaseId
            LEFT JOIN Users u ON u.UserId = cpm.PrescribedBy
            LEFT JOIN Vitals v ON v.PatientId = p.PatientId
            WHERE c.CaseId = @CaseId", new { CaseId = caseId });

            if (header == null) return null;

            var dto = new PatientCaseViewDto
            {
                CaseId = header.CaseId,
                PatientId = header.PatientId,
                PatientName = header.PatientName,
                AgeYears = header.AgeYears,
                Gender = header.Gender,
                BedNumber = header.BedNumber ?? string.Empty,
                ICU = header.ICU ?? string.Empty,
                AdmitDateTime = header.AdmitDateTime,
                SpokeAddressLine1 = header.SpokeAddressLine1 ?? string.Empty,
                SpokeAddressLine2 = header.SpokeAddressLine2,
                SpokeState = header.SpokeState ?? string.Empty,
                SpokeDistrict = header.SpokeDistrict ?? string.Empty,
                SpokeCity = header.SpokeCity ?? string.Empty,
                SpokePIN = header.SpokePIN ?? string.Empty,
                SpecialistName = header.SpecialistName ?? string.Empty,
                SignaturePath = header.SignaturePath ?? string.Empty,
                Speciality = header.Speciality ?? string.Empty,
                ChiefComplaint = header.ChiefComplaint ?? string.Empty,
                ComplaintDescription = header.ComplaintDescription,
                Query = header.Query,
                Examination = header.Examination,
                Advice = header.Advice,
                ConsultationId = header.ConsultationId,
                VitalUpdate = header.VitalUpdate,
                PrescribedBy = header.PrescribedBy
            };

            dto.Address = string.Join(", ", new[] {
                dto.SpokeAddressLine1,
                dto.SpokeAddressLine2,
                dto.SpokeCity,
                dto.SpokeDistrict,
                dto.SpokeState,
                dto.SpokePIN
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

            // Histories
            var histories = await connection.QueryAsync<CaseHistoryItemDto>(@"SELECT 
                    HistoryType AS HistoryType,
                    ConditionName
                FROM CaseHistory WHERE CaseId = @CaseId", new { CaseId = caseId });

            dto.MedicalHistory = string.Join(",", histories.Where(h => h.HistoryType == "Medical").Select(h => h.ConditionName));
            dto.FamilyHistory = string.Join(",", histories.Where(h => h.HistoryType == "Family").Select(h => h.ConditionName));
            dto.Allergies = string.Join(",", histories.Where(h => h.HistoryType == "Allergy").Select(h => h.ConditionName));

            // Pre admit medications
            var pre = await connection.QueryAsync<PreAdmitMedicationDto>(@"SELECT 
                    Medicine, Frequency, Dose, Type, DurationValue, DurationType,ConsultationId
                FROM CasePreAdmitMedications WHERE CaseId = @CaseId", new { CaseId = caseId });
            dto.PreAdmitMedications = pre.ToList();

            // Latest vitals for the case
            var vital = await connection.QueryFirstOrDefaultAsync<VitalDto>(@"SELECT 
                    Temperature, RespiratoryRate, OxygenSaturation, BloodPressureDIA, BloodPressureSYS,
                    HeartRate, PulseRate, BloodGlucose, FIO2, ETCO2, BMI, RightAtrialPressure,
                    CreatedDate
                FROM Vitals WHERE CaseId = @CaseId ORDER BY CreatedDate DESC LIMIT 1", new { CaseId = caseId });
            dto.LatestVitals = vital;

            // 🟢 Nurse details
            var nurse = await connection.QueryFirstOrDefaultAsync<NurseDetailsDto>(@"SELECT 
                CONCAT(n.Title, ' ', n.FirstName, ' ', COALESCE(n.LastName,'')) AS NurseName,
                CONCAT(n.AddressLine1, ' ', COALESCE(n.AddressLine2, '')) AS NurseAddress,
                st.StateName,
                d.DistrictName,
                ci.CityName,
                n.PIN,
                s.SpokeHospitalName,
                n.Speciality
            FROM Users n
            JOIN Spokes s ON n.SpokeId = s.SpokeId
            LEFT JOIN Cases c ON n.UserId = c.CreatedBy
            LEFT JOIN States st ON st.StateId = n.State
            LEFT JOIN Districts d ON d.DistrictCode = n.District
            LEFT JOIN Cities ci ON ci.CityCode = n.City
            WHERE c.CaseId = @CaseId", new { CaseId = caseId });
            dto.NurseDetails = nurse;

            // Health records
            var records = await connection.QueryAsync<CaseHealthRecordItemDto>(@"SELECT 
                    CaseHealthRecordId, RecordType, FilePath, UploadedDate
                FROM CaseHealthRecords WHERE CaseId = @CaseId ORDER BY UploadedDate DESC", new { CaseId = caseId });
            dto.HealthRecords = records.ToList();

            // Prescribed medications for patient (all latest ongoing/discontinued)
            var meds = await connection.QueryAsync<PatientMedicationItemDto>(@"SELECT 
                    CasePreAdmitMedicationId as MedicationId, Medicine, Frequency, Dose, Type, DurationValue, DurationType,
                    PrescribedBy, ConsultationId, PrescribedDateTime as DateTime, Status
                FROM CasePreAdmitMedications cpm
                WHERE cpm.CaseId = @CaseId
                ORDER BY cpm.PrescribedDateTime DESC", new { CaseId = caseId });
            dto.PrescribedMedications = meds.ToList();

            return dto;
        }


        public async Task<IEnumerable<CasePreAdmitMedicationResponseDto>> GetPreAdmitMedicationsAsync(int caseId)
        {
            using var connection = _context.CreateConnection();
            var sql = @"SELECT 
                    cpm.CasePreAdmitMedicationId,
                    cpm.CaseId,
                    cpm.Medicine,
                    cpm.Frequency,
                    cpm.Dose,
                    cpm.Type,
                    cpm.DurationValue,
                    cpm.DurationType,
                    CONCAT(u.FirstName, ' ', COALESCE(u.LastName,'')) AS PrescribedBy,
                    cpm.ConsultationId,
                    cpm.PrescribedDateTime,
                    cpm.Status
                FROM CasePreAdmitMedications cpm
                LEFT JOIN Users u ON u.UserId = cpm.PrescribedBy
                WHERE cpm.CaseId = @CaseId
                ORDER BY cpm.PrescribedDateTime DESC";

            var rows = await connection.QueryAsync<CasePreAdmitMedicationResponseDto>(sql, new { CaseId = caseId });
            return rows;
        }


        public async Task<bool> ActivateCaseAsync(int caseId, int userId)
        {
            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(@"
                UPDATE Cases 
                SET IsActive = 1, UpdatedDate = NOW()
                WHERE CaseId = @CaseId AND IsActive = 0",
                new { CaseId = caseId, UserId = userId });

            return rows > 0;
        }

        public async Task SavePrescriptionPdfAsync(int caseId, string filePath, string consultationId)
        {
            using var connection = _context.CreateConnection();
            var sql = @"
                INSERT INTO CaseHealthRecords (CaseId, RecordType, FilePath, UploadedDate)
                VALUES (@CaseId, 'Prescription', @FilePath, NOW())";

            await connection.ExecuteAsync(sql, new
            {
                CaseId = caseId,
                FilePath = filePath,
                ConsultationId = consultationId
            });
        }
    }

    internal class CaseViewHeaderRow
    {
        public int CaseId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public int AgeYears { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string? BedNumber { get; set; }
        public string? ICU { get; set; }
        public DateTime AdmitDateTime { get; set; }
        public string? SpokeAddressLine1 { get; set; }
        public string? SpokeAddressLine2 { get; set; }
        public string? SpokeState { get; set; }
        public string? SpokeDistrict { get; set; }
        public string? SpokeCity { get; set; }
        public string? SpokePIN { get; set; }
        public string? SpecialistName { get; set; }
        public string? SignaturePath { get; set; }
        public string? Speciality { get; set; }
        public string? ChiefComplaint { get; set; }
        public string? ComplaintDescription { get; set; }
        public string? Query { get; set; }
        public string? Examination { get; set; }
        public string? Advice { get; set; }
        public string? ConsultationId { get; set; }
        public DateTime VitalUpdate { get; set; }
        public int PrescribedBy { get; set; }
    }
}