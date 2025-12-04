namespace TeleICU.API.DTOs.CallDtos;

/// <summary>
/// DTO for call status updates
/// </summary>
public class CallStatusDto
{
    public string CallId { get; set; } = string.Empty;
    public CallStatus Status { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum CallStatus
{
    Initiated = 1,
    Ringing = 2,
    Connected = 3,
    Ended = 4,
    Rejected = 5,
    Missed = 6,
    Failed = 7,
    Disconnected = 8  // Call disconnected but can reconnect within 30 minutes
}
