using TeleICU.API.DTOs.PrescriptionDtos;

namespace TeleICU.API.Services.Interface
{
    public interface IPrescriptionService
    {
        Task<SyncPrescriptionDto?> SyncPrescriptionAsync(SyncPrescriptionDto dto, int prescribedBy, string? encounterId);
        Task<string> GenerateAndSavePrescriptionPdfAsync(string callId, int caseId, string baseUrl);
        Task<string> GenerateAndSavePrescriptionPdfByConsultationIdAsync(string consultationId, string baseUrl);
    }
}
