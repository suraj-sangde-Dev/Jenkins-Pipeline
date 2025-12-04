using TeleICU.API.DTOs.CaseDto;

namespace TeleICU.API.Services.Interface
{
    public interface ICaseService
    {
        Task<int> CreateCaseAsync(CreateCaseDto request, int createdBy);
        Task AddHistoryAsync(CaseHistoryDto request);
        Task AddMedicationsAsync(IEnumerable<PreAdmitMedicationDto> requests);
        Task AddVitalAsync(VitalDto request);
        Task UploadHealthRecordsAsync(CaseHealthRecordUploadDto request, List<string> filePathUrls);

        Task AddCaseQueryAsync(CaseQueryDto dto);
        Task<PatientCaseViewDto?> GetCaseViewAsync(int caseId);
        Task<bool> ActivateCaseAsync(int caseId, int userId);
        Task<IEnumerable<CasePreAdmitMedicationResponseDto>> GetPreAdmitMedicationsAsync(int caseId);
    }
}
