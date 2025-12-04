using TeleICU.API.DTOs.PrescriptionDtos;
using TeleICU.API.Repository.Interface;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Services
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IPrescriptionRepository _repo;
        private readonly IPrescriptionPdfService _pdfService;
        private readonly ICaseRepository _caseRepository;
        private readonly ICallRepository _callRepository;
        private readonly ILogger<PrescriptionService> _logger;

        public PrescriptionService(
            IPrescriptionRepository repo,
            IPrescriptionPdfService pdfService,
            ICaseRepository caseRepository,
            ICallRepository callRepository,
            ILogger<PrescriptionService> logger)
        {
            _repo = repo;
            _pdfService = pdfService;
            _caseRepository = caseRepository;
            _callRepository = callRepository;
            _logger = logger;
        }

        public async Task<SyncPrescriptionDto?> SyncPrescriptionAsync(SyncPrescriptionDto dto, int prescribedBy, string? encounterId)
        {
            // Update Examination and Advice
            await _repo.UpdateCaseExaminationAndAdviceAsync(dto.CaseId, dto.Examination, dto.Advice);

            // Set PrescribedBy for all medications (same as CasesController.AddMedications)
            foreach (var medication in dto.Medications)
            {
                medication.CaseId = dto.CaseId;
                if (medication.PrescribedBy == 0)
                    medication.PrescribedBy = prescribedBy;
            }

            // Use AddSyncedMedicationsAsync which sets IsSynced and SyncedTime when encounterId is provided
            await _repo.AddSyncedMedicationsAsync(dto.Medications, encounterId);

            return dto;
        }

        public async Task<string> GenerateAndSavePrescriptionPdfAsync(string callId, int caseId, string baseUrl)
        {
            try
            {
                // Generate PDF
                var pdfBytes = await _pdfService.GeneratePrescriptionPdfAsync(callId, caseId);

                // Get consultation ID from call data first
                var consultationId = await _callRepository.GetConsultationIdFromCallIdAsync(callId);
                
                // If no ConsultationId found, use CallId as fallback
                if (string.IsNullOrEmpty(consultationId))
                {
                    consultationId = callId;
                }

                // Save to file system using ConsultationId in filename
                var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "Prescriptions");
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                // Sanitize ConsultationId for filename (replace / with _)
                var sanitizedConsultationId = consultationId.Replace("/", "_").Replace("\\", "_");
                var fileName = $"Prescription_{sanitizedConsultationId}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                var filePath = Path.Combine(uploadFolder, fileName);
                await File.WriteAllBytesAsync(filePath, pdfBytes);

                // Save to database
                var filePathUrl = $"{baseUrl}/uploads/Prescriptions/{fileName}";
                await _caseRepository.SavePrescriptionPdfAsync(caseId, filePathUrl, consultationId);

                _logger.LogInformation("Prescription PDF generated and saved for CallId: {CallId}, CaseId: {CaseId}", callId, caseId);
                return filePathUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating prescription PDF for CallId: {CallId}, CaseId: {CaseId}", callId, caseId);
                throw;
            }
        }

        public async Task<string> GenerateAndSavePrescriptionPdfByConsultationIdAsync(string consultationId, string baseUrl)
        {
            try
            {
                // Get CallId and CaseId from ConsultationId
                var consultationInfo = await _callRepository.GetCallIdAndCaseIdFromConsultationIdAsync(consultationId);
                if (consultationInfo == null)
                {
                    throw new Exception($"Consultation with ID '{consultationId}' not found");
                }

                // Use the existing method with CallId and CaseId
                return await GenerateAndSavePrescriptionPdfAsync(consultationInfo.CallId, consultationInfo.CaseId, baseUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating prescription PDF for ConsultationId: {ConsultationId}", consultationId);
                throw;
            }
        }
    }
}
