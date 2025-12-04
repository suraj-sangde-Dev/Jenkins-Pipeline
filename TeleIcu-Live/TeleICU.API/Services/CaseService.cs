using TeleICU.API.DTOs.CaseDto;
using TeleICU.API.Repository.Interface;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Services
{
    public class CaseService : ICaseService
    {
        private readonly ICaseRepository _repository;

        public CaseService(ICaseRepository repository)
        {
            _repository = repository;
        }

        public Task<int> CreateCaseAsync(CreateCaseDto request, int createdBy) =>
            _repository.CreateCaseAsync(request, createdBy);

        public Task AddHistoryAsync(CaseHistoryDto request) =>
            _repository.AddHistoryAsync(request);

        public Task AddMedicationsAsync(IEnumerable<PreAdmitMedicationDto> requests) =>
            _repository.AddMedicationsAsync(requests);

        public Task AddVitalAsync(VitalDto request) =>
            _repository.AddVitalAsync(request);

        public async Task UploadHealthRecordsAsync(CaseHealthRecordUploadDto request, List<string> filePathUrls)
        {
            if (request.RecordTypes == null || request.RecordTypes.Count != filePathUrls.Count)
            {
                throw new ArgumentException("RecordTypes count must match the number of uploaded files.");
            }

            for (int i = 0; i < filePathUrls.Count; i++)
            {
                var fileUrl = filePathUrls[i];
                var recordType = request.RecordTypes[i];
                await _repository.AddHealthRecordAsync(request.CaseId, recordType, fileUrl);
            }
        }

        public async Task AddCaseQueryAsync(CaseQueryDto dto)
        {
            await _repository.AddCaseQueryAsync(dto);
        }

        public Task<PatientCaseViewDto?> GetCaseViewAsync(int caseId) =>
            _repository.GetCaseViewAsync(caseId);

        public Task<bool> ActivateCaseAsync(int caseId, int userId) =>
    _repository.ActivateCaseAsync(caseId, userId);

        public Task<IEnumerable<CasePreAdmitMedicationResponseDto>> GetPreAdmitMedicationsAsync(int caseId) =>
            _repository.GetPreAdmitMedicationsAsync(caseId);
    }
}
