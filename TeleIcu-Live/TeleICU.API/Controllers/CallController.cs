using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlX.XDevAPI.Common;
using System.Security.Claims;
using TeleICU.API.DTOs.CallDtos;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CallController : ControllerBase
{
    private readonly ICallService _callService;
    private readonly ILogger<CallController> _logger;

    public CallController(ICallService callService, ILogger<CallController> logger)
    {
        _callService = callService;
        _logger = logger;
    }

    /// <summary>
    /// Get connection configuration for inVC SDK
    /// Returns the conn_obj required by connectServer() method
    /// </summary>
    /// <param name="encounterUid">Unique meeting/encounter identifier (usually caseId or patientId)</param>
    [HttpGet("connection-config")]
    public async Task<IActionResult> GetConnectionConfig([FromQuery] string encounterUid)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var config = await _callService.GetConnectionConfigAsync(userIdClaim, encounterUid);
            return Ok(new { success = true, data = config });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connection config");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Initiate a call to another user
    /// This creates a call log entry in the database
    /// Actual call signaling happens through SignalR Hub
    /// </summary>
    [HttpPost("initiate")]
    public async Task<IActionResult> InitiateCall([FromBody] CallRequestDto request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CallerId) || string.IsNullOrEmpty(request.CalleeId))
            {
                return BadRequest(new { success = false, message = "Caller and Callee IDs are required" });
            }

            var callId = await _callService.InitiateCallAsync(request);
            return Ok(new
            {
                success = true,
                message = "Call initiated successfully",
                data = new { callId }
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in call request");
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating call");
            return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Get call history for the authenticated user
    /// </summary>
    /// <param name="limit">Maximum number of records to return (default: 50)</param>
    [HttpGet("history")]
    public async Task<IActionResult> GetCallHistory([FromQuery] int limit = 50)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var history = await _callService.GetCallHistoryAsync(userIdClaim, limit);
            return Ok(new { success = true, data = history });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting call history");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }
    /// <summary>
    /// Get latest call history for the user by caseId
    [HttpGet("latestcallbycaseId")]
    public async Task<IActionResult> GetLatestCallbyCaseId([FromQuery] int caseId)
    {
        try
        {
            var history = await _callService.GetLatestCallAsync(caseId);
            return Ok(new
            {
                data = history,
                message = "callid fetched successfully.",
                statusCode = 200
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting call history");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get active calls for the authenticated user
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveCalls()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var activeCalls = await _callService.GetActiveCallsAsync(userIdClaim);
            return Ok(new { success = true, data = activeCalls });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active calls");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Save call recording URL after upload
    /// Called by client after stopCallRecording() completes
    /// </summary>
    [HttpPost("recording")]
    public async Task<IActionResult> SaveCallRecording([FromBody] SaveRecordingRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CallId) || string.IsNullOrEmpty(request.RecordingUrl))
            {
                return BadRequest(new { success = false, message = "CallId and RecordingUrl are required" });
            }

            await _callService.SaveCallRecordingAsync(request.CallId, request.RecordingUrl);
            return Ok(new { success = true, message = "Recording saved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving call recording");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get consultation summary with filtering and pagination
    /// Supports filtering by Patient, Date, CoE, and Specialist
    /// </summary>
    [HttpGet("consultation-summary")]
    [Authorize(Roles = "nurse,doctor,specialist,coe_admin,state_admin")]
    public async Task<IActionResult> GetConsultationSummary([FromQuery] ConsultationSummaryRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(role))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var userId = int.Parse(userIdClaim);
            var summary = await _callService.GetConsultationSummaryAsync(request, userId, role);

            return Ok(new { 
                success = true, 
                data = summary,
                message = "Consultation summary fetched successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting consultation summary");
            return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
        }
    }
}

public class SaveRecordingRequest
{
    public string CallId { get; set; } = string.Empty;
    public string RecordingUrl { get; set; } = string.Empty;
}
