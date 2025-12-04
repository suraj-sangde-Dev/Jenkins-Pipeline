using Dapper;
using TeleICU.API.DTOs.PatientDto;
using TeleICU.API.DTOs.StateDtos;
using TeleICU.API.Models.AuthModels;
using TeleICU.API.Models.Beds;
using TeleICU.API.Repository.Interface;

namespace TeleICU.API.Repository
{
    public class PatientRepository : IPatientRepository
    {
        private readonly DapperContext _context;

        public PatientRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BedModel>> GetVacantBeds(int spokeId)
        {
            var sql = @"
            SELECT BedId, BedNumber, BedType
            FROM Beds
            WHERE SpokeId = @SpokeId
              AND Status = 'Vacant'
              AND IsUnderMaintenance = FALSE";

            using (var connection = _context.CreateConnection())
            {
                return await connection.QueryAsync<BedModel>(sql, new { SpokeId = spokeId });
            }
        }

        public async Task<int> RegisterPatient(RegisterPatientRequest request, int createdBy, int spokeId)
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var sql = @"
INSERT INTO Patients 
(SpokeId, BedId, FirstName, MiddleName, LastName, DOB, Gender, WeightInKg, BloodGroup, DoctorId, Phone, Email, 
 AddressLine1, AddressLine2, State, District, City, Pin, CreatedBy)
VALUES 
(@SpokeId, @BedId, @FirstName, @MiddleName, @LastName, @DOB, @Gender, @WeightInKg, @BloodGroup, @DoctorId, @Phone, @Email, 
 @AddressLine1, @AddressLine2, @State, @District, @City, @Pin, @CreatedBy);
SELECT LAST_INSERT_ID();";

                    var patientId = await connection.ExecuteScalarAsync<int>(sql, new
                    {
                        SpokeId = spokeId, // 🔹 Force SpokeId from logged-in user
                        request.BedId,
                        request.FirstName,
                        request.MiddleName,
                        request.LastName,
                        request.DOB,
                        request.Gender,
                        request.WeightInKg,
                        request.BloodGroup,
                        request.DoctorId,
                        request.Phone,
                        request.Email,
                        request.AddressLine1,
                        request.AddressLine2,
                        request.State,
                        request.District,
                        request.City,
                        request.Pin,
                        CreatedBy = createdBy
                    }, transaction);

                    if (request.BedId.HasValue)
                    {
                        var updateBedSql = "UPDATE Beds SET Status = 'Occupied' WHERE BedId = @BedId";
                        await connection.ExecuteAsync(updateBedSql, new { request.BedId }, transaction);
                    }

                    transaction.Commit();
                    return patientId;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }


        public async Task<bool> DischargePatient(int patientId)
        {
            using (var connection = _context.CreateConnection())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var bedId = await connection.ExecuteScalarAsync<int?>(
                            "SELECT BedId FROM Patients WHERE PatientId = @PatientId",
                            new { PatientId = patientId }, transaction);

                        var updatePatientSql = "UPDATE Patients SET BedId = NULL WHERE PatientId = @PatientId";
                        await connection.ExecuteAsync(updatePatientSql, new { PatientId = patientId }, transaction);

                        if (bedId.HasValue)
                        {
                            var updateBedSql = "UPDATE Beds SET Status = 'Vacant' WHERE BedId = @BedId";
                            await connection.ExecuteAsync(updateBedSql, new { BedId = bedId.Value }, transaction);
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<IEnumerable<UserModel>> GetUsersByRoleAndSpoke(int roleId, int spokeId)
        {
            var sql = @"SELECT 
                    UserId, Username, Email, Phone,
                    FirstName, MiddleName, LastName, RoleId, SpokeId
                FROM Users
                WHERE RoleId = @RoleId AND SpokeId = @SpokeId AND IsActive = 1";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<UserModel>(sql, new { RoleId = roleId, SpokeId = spokeId });
        }


        public async Task<IEnumerable<object>> GetPatientsBySpoke(int spokeId)
        {
            var sql = @"
        SELECT 
            p.PatientId,
            CONCAT(p.FirstName, ' ', IFNULL(p.MiddleName,''), ' ', IFNULL(p.LastName,'')) AS PatientName,
            p.Gender,
            TIMESTAMPDIFF(YEAR, p.DOB, CURDATE()) AS AgeYears,
            TIMESTAMPDIFF(MONTH, p.DOB, CURDATE()) % 12 AS AgeMonths,
            p.BloodGroup,
            b.BedNumber,
            DATE_FORMAT(p.AdmitDate, '%d-%m-%Y') AS AdmittedOn,
            CONCAT(d.FirstName, ' ', IFNULL(d.LastName,'')) AS DoctorName, c.CaseId
        FROM Patients p
        LEFT JOIN Beds b ON p.BedId = b.BedId
        LEFT JOIN Users d ON p.DoctorId = d.UserId
        LEFT JOIN Cases c ON p.PatientId = c.PatientId
        WHERE p.SpokeId = @SpokeId
        ORDER BY p.PatientId DESC;
    ";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<object>(sql, new { SpokeId = spokeId });
        }

        public async Task<IEnumerable<object>> GetPatientsByCoe(int coeId)
        {
            var sql = @"
                SELECT 
                    p.PatientId,
                    CONCAT(p.FirstName, ' ', IFNULL(p.MiddleName,''), ' ', IFNULL(p.LastName,'')) AS PatientName,
                    p.Gender,
                    TIMESTAMPDIFF(YEAR, p.DOB, CURDATE()) AS AgeYears,
                    TIMESTAMPDIFF(MONTH, p.DOB, CURDATE()) % 12 AS AgeMonths,
                    p.BloodGroup,
                    b.BedNumber,
                    DATE_FORMAT(p.AdmitDate, '%d-%m-%Y') AS AdmittedOn,
                    CONCAT(d.FirstName, ' ', IFNULL(d.LastName,'')) AS DoctorName,
                    c.CaseId
                FROM Patients p
                LEFT JOIN Beds b ON p.BedId = b.BedId
                LEFT JOIN Users d ON p.DoctorId = d.UserId
                LEFT JOIN Spokes s ON p.SpokeId = s.SpokeId
                -- Case details
                LEFT JOIN Cases c ON p.PatientId = c.PatientId
            -- FILTER BY COE
                WHERE s.CoeId = @CoeId
                ORDER BY p.PatientId DESC;
                    ";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<object>(sql, new { CoeId = coeId });
        }

        public async Task<IEnumerable<PatientTileDto>> GetPatientTilesBySpoke(int spokeId)
        {
            var sql = @"
            SELECT 
                p.PatientId,
                c.CaseId,
                CONCAT(p.FirstName, ' ', IFNULL(p.MiddleName,''), ' ', IFNULL(p.LastName,'')) AS PatientName,
                s.SpokeHospitalName,
                p.Gender,
                TIMESTAMPDIFF(YEAR, p.DOB, CURDATE()) AS AgeYears,
                TIMESTAMPDIFF(MONTH, p.DOB, CURDATE()) % 12 AS AgeMonths,
                p.BloodGroup,
                b.BedNumber,
                DATE_FORMAT(p.AdmitDate, '%d-%m-%Y') AS AdmittedOn,
                DATE_FORMAT(v.UpdatedDate, '%d-%m-%Y %h:%i:%s %p') AS UpdatedVital,
                CONCAT(d.FirstName, ' ', IFNULL(d.LastName,'')) AS DoctorName,
                v.Temperature,
                v.RespiratoryRate,
                v.OxygenSaturation,
                v.BloodPressureSYS,
                v.BloodPressureDIA,
                v.HeartRate,
                p.CreatedBy,
                m.PrescribedBy as lastConsultedSpecialist
            FROM Patients p
            LEFT JOIN Beds b ON p.BedId = b.BedId
            LEFT JOIN Users d ON p.DoctorId = d.UserId
            LEFT JOIN Spokes s ON p.SpokeId = s.SpokeId

            -- Latest CaseId per patient
            LEFT JOIN (
            SELECT cc.PatientId, MAX(cc.CaseId) AS CaseId
            FROM Cases cc
            GROUP BY cc.PatientId
            ) c ON c.PatientId = p.PatientId

            -- Latest vitals per patient
            LEFT JOIN (
                SELECT vv.*
                FROM Vitals vv
                INNER JOIN (
                    SELECT PatientId, MAX(CreatedDate) AS Latest
                    FROM Vitals
                    GROUP BY PatientId
                ) latest ON vv.PatientId = latest.PatientId 
                AND vv.CreatedDate = latest.Latest
                ) v ON v.PatientId = p.PatientId

            -- Latest PrescribedBy for each CaseId
            LEFT JOIN (
                SELECT cpam.CaseId, cpam.PrescribedBy
                FROM CasePreAdmitMedications cpam
                INNER JOIN (
                    SELECT CaseId, MAX(CasePreAdmitMedicationId) AS MaxId
                    FROM CasePreAdmitMedications
                    GROUP BY CaseId
                    ) latestMed ON cpam.CasePreAdmitMedicationId = latestMed.MaxId
                    ) m ON m.CaseId = c.CaseId

                WHERE p.SpokeId = @spokeId
                ORDER BY p.PatientId DESC;";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<PatientTileDto>(sql, new { CoeId = spokeId });
        }

        public async Task<IEnumerable<PatientTileDto>> GetPatientTilesByCoe(int coeId)
        {
            var sql = @"
            SELECT 
                p.PatientId,
                c.CaseId,
                CONCAT(p.FirstName, ' ', IFNULL(p.MiddleName,''), ' ', IFNULL(p.LastName,'')) AS PatientName,
                s.SpokeHospitalName,
                p.Gender,
                TIMESTAMPDIFF(YEAR, p.DOB, CURDATE()) AS AgeYears,
                TIMESTAMPDIFF(MONTH, p.DOB, CURDATE()) % 12 AS AgeMonths,
                p.BloodGroup,
                b.BedNumber,
                DATE_FORMAT(p.AdmitDate, '%d-%m-%Y') AS AdmittedOn,
                DATE_FORMAT(v.UpdatedDate, '%d-%m-%Y %h:%i:%s %p') AS UpdatedVital,
                CONCAT(d.FirstName, ' ', IFNULL(d.LastName,'')) AS DoctorName,
                v.Temperature,
                v.RespiratoryRate,
                v.OxygenSaturation,
                v.BloodPressureSYS,
                v.BloodPressureDIA,
                v.HeartRate,
                p.CreatedBy,
                m.PrescribedBy as lastConsultedSpecialist
            FROM Patients p
            LEFT JOIN Beds b ON p.BedId = b.BedId
            LEFT JOIN Users d ON p.DoctorId = d.UserId
            LEFT JOIN Spokes s ON p.SpokeId = s.SpokeId

            -- Latest CaseId per patient
            LEFT JOIN (
            SELECT cc.PatientId, MAX(cc.CaseId) AS CaseId
            FROM Cases cc
            GROUP BY cc.PatientId
            ) c ON c.PatientId = p.PatientId

            -- Latest vitals per patient
            LEFT JOIN (
            SELECT vv.*
                FROM Vitals vv
                INNER JOIN (
                 SELECT PatientId, MAX(CreatedDate) AS Latest
                 FROM Vitals
                 GROUP BY PatientId
                 ) latest ON vv.PatientId = latest.PatientId 
                 AND vv.CreatedDate = latest.Latest
                 ) v ON v.PatientId = p.PatientId

                 -- Latest PrescribedBy for each Case
                LEFT JOIN (
                SELECT cpam.CaseId, cpam.PrescribedBy
                FROM CasePreAdmitMedications cpam
                INNER JOIN (
                SELECT CaseId, MAX(CasePreAdmitMedicationId) AS MaxId
                FROM CasePreAdmitMedications
                GROUP BY CaseId
                ) latestMed ON cpam.CasePreAdmitMedicationId = latestMed.MaxId
                ) m ON m.CaseId = c.CaseId

                WHERE s.CoeId = @CoeId
                ORDER BY p.PatientId DESC;";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<PatientTileDto>(sql, new { CoeId = coeId });
        }


        public async Task<IEnumerable<VitalTrendDto>> GetVitalTrendsByPatient(int patientId, DateTime? fromDate)
        {
            var sql = @"
SELECT 
    VitalId,
    PatientId,
    Temperature,
    RespiratoryRate,
    OxygenSaturation,
    BloodPressureDIA,
    BloodPressureSYS,
    HeartRate,
    PulseRate,
    BloodGlucose,
    FIO2,
    ETCO2,
    BMI,
    RightAtrialPressure,
    CreatedDate,
    UpdatedDate
FROM Vitals 
WHERE PatientId = @PatientId";

            if (fromDate.HasValue)
            {
                sql += " AND CreatedDate >= @FromDate";
            }

            sql += " ORDER BY CreatedDate ASC";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<VitalTrendDto>(sql, new { 
                PatientId = patientId, 
                FromDate = fromDate 
            });
        }

        public async Task<string> GetPatientNameById(int patientId)
        {
            var sql = @"
SELECT CONCAT(FirstName, ' ', IFNULL(MiddleName,''), ' ', IFNULL(LastName,'')) AS PatientName
FROM Patients 
WHERE PatientId = @PatientId";

            using var connection = _context.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<string>(sql, new { PatientId = patientId }) ?? string.Empty;
        }

        public async Task<IEnumerable<PatientMedicationDto>> GetPatientMedications(int patientId)
        {
            var sql = @"
SELECT 
    cpm.CasePreAdmitMedicationId as MedicationId,
    cpm.CaseId,
    cpm.Medicine,
    cpm.Frequency,
    cpm.Dose,
    cpm.Type,
    cpm.DurationValue,
    cpm.DurationType,
    cpm.PrescribedBy,
    cpm.ConsultationId,
    cpm.CallId,
    cpm.PrescribedDateTime as DateTime,
    cpm.Status,
    cpm.IsSynced,
    cpm.SyncedTime
FROM CasePreAdmitMedications cpm
JOIN Cases c ON cpm.CaseId = c.CaseId
WHERE c.PatientId = @PatientId AND cpm.Status = 'Ongoing'
ORDER BY cpm.PrescribedDateTime DESC, cpm.CasePreAdmitMedicationId DESC";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<PatientMedicationDto>(sql, new { PatientId = patientId });
        }

        public async Task AddVitalForPatient(int patientId, TeleICU.API.DTOs.CaseDto.VitalDto vital)
        {
            using var connection = _context.CreateConnection();
            // Resolve latest case for the patient (fallback to NULL if no case)
            var caseId = await connection.ExecuteScalarAsync<int?>(
                "SELECT CaseId FROM Cases WHERE PatientId = @PatientId ORDER BY CreatedDate DESC LIMIT 1",
                new { PatientId = patientId }
            );

            //// Compute condition
            //string condition = ComputeCondition(vital);

            //// Check column existence
            //var hasConditionColumn = await connection.ExecuteScalarAsync<int>(
            //    @"SELECT COUNT(*) FROM information_schema.COLUMNS 
            //                      WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Vitals' AND COLUMN_NAME = 'Condition'"
            //) > 0;

            //if (hasConditionColumn)
            //{
            //    await connection.ExecuteAsync(@"
            //    INSERT INTO Vitals 
            //    (PatientId, CaseId, Temperature, RespiratoryRate, OxygenSaturation, BloodPressureDIA, BloodPressureSYS,
            //     HeartRate, PulseRate, BloodGlucose, FIO2, ETCO2, BMI, RightAtrialPressure, Condition)
            //    VALUES (@PatientId, @CaseId, @Temperature, @RespiratoryRate, @OxygenSaturation, @BloodPressureDIA, @BloodPressureSYS,
            //            @HeartRate, @PulseRate, @BloodGlucose, @FIO2, @ETCO2, @BMI, @RightAtrialPressure, @Condition)",
            //        new
            //        {
            //            PatientId = patientId,
            //            CaseId = caseId,
            //            vital.Temperature,
            //            vital.RespiratoryRate,
            //            vital.OxygenSaturation,
            //            vital.BloodPressureDIA,
            //            vital.BloodPressureSYS,
            //            vital.HeartRate,
            //            vital.PulseRate,
            //            vital.BloodGlucose,
            //            vital.FIO2,
            //            vital.ETCO2,
            //            vital.BMI,
            //            vital.RightAtrialPressure,
            //            Condition = condition
            //        });

            //    var hasCurrentCondition = await connection.ExecuteScalarAsync<int>(
            //        @"SELECT COUNT(*) FROM information_schema.COLUMNS 
            //                          WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Patients' AND COLUMN_NAME = 'CurrentCondition'"
            //    ) > 0;
            //    if (hasCurrentCondition)
            //    {
            //        await connection.ExecuteAsync("UPDATE Patients SET CurrentCondition = @Condition, UpdatedDate = NOW() WHERE PatientId = @PatientId",
            //            new { Condition = condition, PatientId = patientId });
            //    }
            //}
            //else
            //{
                await connection.ExecuteAsync(@"
INSERT INTO Vitals 
(PatientId, CaseId, Temperature, RespiratoryRate, OxygenSaturation, BloodPressureDIA, BloodPressureSYS,
 HeartRate, PulseRate, BloodGlucose, FIO2, ETCO2, BMI, RightAtrialPressure)
VALUES (@PatientId, @CaseId, @Temperature, @RespiratoryRate, @OxygenSaturation, @BloodPressureDIA, @BloodPressureSYS,
        @HeartRate, @PulseRate, @BloodGlucose, @FIO2, @ETCO2, @BMI, @RightAtrialPressure)",
                    new
                    {
                        PatientId = patientId,
                        CaseId = caseId,
                        vital.Temperature,
                        vital.RespiratoryRate,
                        vital.OxygenSaturation,
                        vital.BloodPressureDIA,
                        vital.BloodPressureSYS,
                        vital.HeartRate,
                        vital.PulseRate,
                        vital.BloodGlucose,
                        vital.FIO2,
                        vital.ETCO2,
                        vital.BMI,
                        vital.RightAtrialPressure
                    });
            }
     //   }

        //private static string ComputeCondition(TeleICU.API.DTOs.CaseDto.VitalDto v)
        //{
        //    bool critical =
        //        v.OxygenSaturation < 90 ||
        //        v.BloodPressureSYS < 90 || v.BloodPressureSYS > 180 ||
        //        v.HeartRate < 40 || v.HeartRate > 130 ||
        //        v.RespiratoryRate < 8 || v.RespiratoryRate > 30 ||
        //        v.Temperature > 103 || v.Temperature < 95;
        //    if (critical) return "Critical";

        //    bool severe =
        //        (v.OxygenSaturation >= 90 && v.OxygenSaturation < 94) ||
        //        (v.BloodPressureSYS >= 160 && v.BloodPressureSYS <= 180) || (v.BloodPressureSYS >= 90 && v.BloodPressureSYS <= 100) ||
        //        (v.HeartRate >= 110 && v.HeartRate <= 130) || (v.HeartRate >= 40 && v.HeartRate <= 50) ||
        //        (v.RespiratoryRate >= 20 && v.RespiratoryRate <= 30) || (v.RespiratoryRate >= 8 && v.RespiratoryRate <= 10) ||
        //        (v.Temperature >= 100 && v.Temperature <= 103);
        //    if (severe) return "Severe";

        //    return "Stable";
        //}

        public async Task<PatientDetailDto?> GetPatientById(int patientId)
        {
            var sql = @"
SELECT 
    p.PatientId,
    p.SpokeId,
    p.BedId,
    b.BedNumber,
    p.DoctorId,
    CONCAT(d.FirstName, ' ', IFNULL(d.LastName,'')) AS DoctorName,
    p.FirstName,
    p.MiddleName,
    p.LastName,
    CONCAT(p.FirstName, ' ', IFNULL(p.MiddleName,''), ' ', IFNULL(p.LastName,'')) AS PatientName,
    p.DOB,
    TIMESTAMPDIFF(YEAR, p.DOB, CURDATE()) AS AgeYears,
    TIMESTAMPDIFF(MONTH, p.DOB, CURDATE()) % 12 AS AgeMonths,
    p.Gender,
    p.WeightInKg,
    p.BloodGroup,
    p.Phone,
    p.Email,
    p.AddressLine1,
    p.AddressLine2,
    (SELECT MAX(c.CaseId) FROM Cases c WHERE c.PatientId = p.PatientId) AS CaseId,
    p.Pin,
    p.AdmitDate,
    p.CreatedDate,
    p.UpdatedDate,
    p.CreatedBy,
    s.StateId AS StateId, s.StateCode AS StateCode, s.StateName AS StateName,
    di.DistrictCode AS DistrictCode, di.DistrictName AS DistrictName,
    ci.CityCode AS CityCode, ci.CityName AS CityName
FROM Patients p
LEFT JOIN Beds b ON p.BedId = b.BedId
LEFT JOIN Users d ON p.DoctorId = d.UserId
LEFT JOIN States s ON p.State = s.StateCode
LEFT JOIN Districts di ON p.District = di.DistrictCode
LEFT JOIN Cities ci ON p.City = ci.CityCode
WHERE p.PatientId = @PatientId";

            using var connection = _context.CreateConnection();
            var result = await connection.QueryAsync<PatientDetailDto, StateDto, DistrictDto, CityDto, PatientDetailDto>(
                sql,
                (patient, state, district, city) =>
                {
                    patient.State = state;
                    patient.District = district;
                    patient.City = city;
                    return patient;
                },
                new { PatientId = patientId },
                splitOn: "StateId,DistrictCode,CityCode"
            );
            return result.FirstOrDefault();
        }

        public async Task<bool> UpdatePatient(EditPatientRequest request)
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            
            try
            {
                // Get current patient data to check bed changes
                var currentPatient = await connection.QuerySingleOrDefaultAsync<dynamic>(
                    "SELECT BedId, SpokeId FROM Patients WHERE PatientId = @PatientId",
                    new { PatientId = request.PatientId }, transaction);

                if (currentPatient == null)
                    return false;

                // Update patient data
                var updatePatientSql = @"
UPDATE Patients 
SET BedId = @BedId,
    DoctorId = @DoctorId,
    FirstName = @FirstName,
    MiddleName = @MiddleName,
    LastName = @LastName,
    DOB = @DOB,
    Gender = @Gender,
    WeightInKg = @WeightInKg,
    BloodGroup = @BloodGroup,
    Phone = @Phone,
    Email = @Email,
    AddressLine1 = @AddressLine1,
    AddressLine2 = @AddressLine2,
    State = @State,
    District = @District,
    City = @City,
    Pin = @Pin,
    UpdatedDate = NOW()
WHERE PatientId = @PatientId";

                await connection.ExecuteAsync(updatePatientSql, request, transaction);

                // Handle bed changes
                var oldBedId = currentPatient.BedId;
                var newBedId = request.BedId;

                // If bed changed, update bed statuses
                if (oldBedId != newBedId)
                {
                    // Free old bed if exists
                    if (oldBedId != null)
                    {
                        await connection.ExecuteAsync(
                            "UPDATE Beds SET Status = 'Vacant' WHERE BedId = @BedId",
                            new { BedId = oldBedId }, transaction);
                    }

                    // Occupy new bed if assigned
                    if (newBedId != null)
                    {
                        await connection.ExecuteAsync(
                            "UPDATE Beds SET Status = 'Occupied' WHERE BedId = @BedId",
                            new { BedId = newBedId }, transaction);
                    }
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
