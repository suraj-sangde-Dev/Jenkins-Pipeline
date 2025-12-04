using TeleICU.API.DTOs.CallDtos;

namespace TeleICU.API.Repository.Interface;

public interface ICallRepository
{
    Task<string> CreateCallLogAsync(CallRequestDto request);
    Task UpdateCallStatusAsync(string callId, CallStatus status);
    Task UpdateCallEndTimeAsync(string callId, DateTime endTime);
    Task<object?> GetCallHistoryAsync(string userId, int limit = 50);
    Task<object?> GetLatestCallAsync(int caseId); 
    Task<object?> GetActiveCallsAsync(string userId);
    Task SaveCallRecordingAsync(string callId, string recordingUrl);
    Task<ConsultationSummaryResponse> GetConsultationSummaryAsync(ConsultationSummaryRequest request, int userId, string role);
    Task<CallPrescriptionDataDto?> GetCallDataForPrescriptionAsync(string callId);
    Task<ConsultationInfoDto?> GetCallIdAndCaseIdFromConsultationIdAsync(string consultationId);
    Task<string?> GetConsultationIdFromCallIdAsync(string callId);
}
