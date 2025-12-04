using TeleICU.API.DTOs.CallDtos;

namespace TeleICU.API.Services.Interface;

public interface ICallService
{
    Task<CallConnectionDto> GetConnectionConfigAsync(string userId, string encounterUid);
    Task<string> InitiateCallAsync(CallRequestDto request);
    Task<object?> GetCallHistoryAsync(string userId, int limit = 50);
    Task<object?> GetLatestCallAsync(int caseId);
    Task<object?> GetActiveCallsAsync(string userId);
    Task SaveCallRecordingAsync(string callId, string recordingUrl);
    Task<ConsultationSummaryResponse> GetConsultationSummaryAsync(ConsultationSummaryRequest request, int userId, string role);
    Task UpdateCallStatusAsync(string callId, CallStatus status);
    Task UpdateCallEndTimeAsync(string callId, DateTime endTime);

}
