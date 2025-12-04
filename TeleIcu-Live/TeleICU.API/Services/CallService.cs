using TeleICU.API.DTOs.CallDtos;
using TeleICU.API.Repository.Interface;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Services;

public class CallService : ICallService
{
    private readonly ICallRepository _callRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CallService> _logger;

    public CallService(
        ICallRepository callRepository,
        IConfiguration configuration,
        ILogger<CallService> logger)
    {
        _callRepository = callRepository;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Get connection configuration for inVC SDK
    /// This provides the conn_obj required by connectServer()
    /// </summary>
    public async Task<CallConnectionDto> GetConnectionConfigAsync(string userId, string encounterUid)
    {
        // Get configuration from appsettings.json
        var serverUrl = _configuration["InVC:ServerURL"] ?? "https://testcdac.invc..vc";
        var authToken = _configuration["InVC:AuthToken"] ?? "";
        var projectId = _configuration["InVC:ProjectId"] ?? "";

        var config = new CallConnectionDto
        {
            ServerURL = serverUrl,
            AuthToken = authToken,
            AppName = "TeleICU",
            SelfName = userId,
            EncounterUid = encounterUid,
            Uid = userId,
            ProjectId = projectId
        };

        _logger.LogInformation("Generated connection config for user {UserId}", userId);
        return await Task.FromResult(config);
    }

    /// <summary>
    /// Initiate a new call and create log entry
    /// </summary>
    public async Task<string> InitiateCallAsync(CallRequestDto request)
    {
        try
        {
            var callId = await _callRepository.CreateCallLogAsync(request);
            _logger.LogInformation("Call initiated: {CallId} from {Caller} to {Callee}", 
                callId, request.CallerId, request.CalleeId);
            return callId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating call");
            throw;
        }
    }

    /// <summary>
    /// Get call history for a user
    /// </summary>
    public async Task<object?> GetCallHistoryAsync(string userId, int limit = 50)
    {
        return await _callRepository.GetCallHistoryAsync(userId, limit);
    }

    /// <summary>
    /// Get latest call history by caseId
    public async Task<object> GetLatestCallAsync(int caseId)
    {
        return await _callRepository.GetLatestCallAsync(caseId);
    }

    /// <summary>
    /// Get active calls for a user
    /// </summary>
    public async Task<object?> GetActiveCallsAsync(string userId)
    {
        return await _callRepository.GetActiveCallsAsync(userId);
    }

    /// <summary>
    /// Save call recording URL
    /// </summary>
    public async Task SaveCallRecordingAsync(string callId, string recordingUrl)
    {
        await _callRepository.SaveCallRecordingAsync(callId, recordingUrl);
        _logger.LogInformation("Call recording saved for {CallId}", callId);
    }

    /// <summary>
    /// Get consultation summary with filtering and pagination
    /// </summary>
    public async Task<ConsultationSummaryResponse> GetConsultationSummaryAsync(ConsultationSummaryRequest request, int userId, string role)
    {
        return await _callRepository.GetConsultationSummaryAsync(request, userId, role);
    }

    public async Task UpdateCallStatusAsync(string callId, CallStatus status)
    {
        await _callRepository.UpdateCallStatusAsync(callId, status);
    }

    public async Task UpdateCallEndTimeAsync(string callId, DateTime endTime)
    {
        await _callRepository.UpdateCallEndTimeAsync(callId, endTime);
    }

}
