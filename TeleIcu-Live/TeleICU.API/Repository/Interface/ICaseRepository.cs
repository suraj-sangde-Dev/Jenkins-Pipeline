using TeleICU.API.DTOs.CaseDto;

namespace TeleICU.API.Repository.Interface
{
    public interface ICaseRepository
    {
        Task<int> CreateCaseAsync(CreateCaseDto request, int createdBy);
        Task AddHistoryAsync(CaseHistoryDto request);
        Task AddMedicationsAsync(IEnumerable<PreAdmitMedicationDto> requests);
        Task AddVitalAsync(VitalDto request);
        Task AddHealthRecordAsync(int caseId, string recordType, string filePathUrl);
        Task AddCaseQueryAsync(CaseQueryDto dto);
        Task<PatientCaseViewDto?> GetCaseViewAsync(int caseId);
        Task<bool> ActivateCaseAsync(int caseId, int userId);
        Task<IEnumerable<CasePreAdmitMedicationResponseDto>> GetPreAdmitMedicationsAsync(int caseId);
        Task SavePrescriptionPdfAsync(int caseId, string filePath, string consultationId);
    }
}
