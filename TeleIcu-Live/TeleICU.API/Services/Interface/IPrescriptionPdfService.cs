namespace TeleICU.API.Services;

public interface IPrescriptionPdfService
{
    Task<byte[]> GeneratePrescriptionPdfAsync(string callId, int caseId);
}


