using TeleICU.API.DTOs.CaseDto;

namespace TeleICU.API.Repository.Interface
{
    public interface IPrescriptionRepository
    {
        Task<bool> UpdateCaseExaminationAndAdviceAsync(int caseId, string? examination, string? advice);
        Task AddSyncedMedicationsAsync(IEnumerable<PreAdmitMedicationDto> medications, string? encounterId);
    }
}
