using Dapper;
using TeleICU.API.DTOs.CaseDto;
using TeleICU.API.Helpers;
using TeleICU.API.Repository.Interface;

namespace TeleICU.API.Repository
{
    public class PrescriptionRepository : IPrescriptionRepository
    {
        private readonly DapperContext _context;

        public PrescriptionRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<bool> UpdateCaseExaminationAndAdviceAsync(int caseId, string? examination, string? advice)
        {
            var sql = @"
                UPDATE Cases 
                SET Examination = @Examination,
                    Advice = @Advice,
                    UpdatedDate = NOW()
                WHERE CaseId = @CaseId";

            using var connection = _context.CreateConnection();
            var result = await connection.ExecuteAsync(sql, new
            {
                CaseId = caseId,
                Examination = examination,
                Advice = advice
            });
            return result > 0;
        }

        public async Task AddSyncedMedicationsAsync(IEnumerable<PreAdmitMedicationDto> medications, string? encounterId)
        {
            if (medications == null) return;
            var valid = medications.Where(r => r != null).ToList();
            if (valid.Count == 0) return;

            using var connection = _context.CreateConnection();
            //var caseIds = valid.Select(r => r.CaseId).Distinct().ToList();
            
            //// Delete existing medications for the case(s)
            //foreach (var cid in caseIds)
            //{
            //    await connection.ExecuteAsync("DELETE FROM CasePreAdmitMedications WHERE CaseId = @CaseId", new { CaseId = cid });
            //}

            // Generate ConsultationId
            var prefix = await connection.ExecuteScalarAsync<string>("SELECT DATE_FORMAT(NOW(), 'CONS/%Y/%m/%d')");
            var currentMax = await connection.ExecuteScalarAsync<int>(
                @"SELECT COALESCE(MAX(CAST(SUBSTRING_INDEX(ConsultationId,'/',-1) AS UNSIGNED)),0)
                  FROM CasePreAdmitMedications
                  WHERE ConsultationId LIKE CONCAT(@Prefix,'/%')",
                new { Prefix = prefix });

            int next = currentMax;
            var syncedTime = DateTime.UtcNow;
            var isSynced = !string.IsNullOrEmpty(encounterId);

            foreach (var r in valid)
            {
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
                CallId = encounterId,
                PrescribedDateTime = syncedTime,
                r.Status,
                IsSynced = isSynced ? 1 : 0,
                SyncedTime = isSynced ? syncedTime : (DateTime?)null
            }).ToList();

            if (items.Count == 0) return;

            var sql = @"INSERT INTO CasePreAdmitMedications 
                        (CaseId, Medicine, Frequency, Dose, Type, DurationValue, DurationType, 
                         PrescribedBy, ConsultationId, CallId, PrescribedDateTime, Status, IsSynced, SyncedTime)
                        VALUES (@CaseId, @Medicine, @Frequency, @Dose, @Type, @DurationValue, @DurationType,
                                @PrescribedBy, @ConsultationId, @CallId, @PrescribedDateTime, @Status, @IsSynced, @SyncedTime)";
            
            await connection.ExecuteAsync(sql, items);
        }
    }
}
