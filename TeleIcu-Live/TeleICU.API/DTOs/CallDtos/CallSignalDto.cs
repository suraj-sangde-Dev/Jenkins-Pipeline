namespace TeleICU.API.DTOs.CallDtos;

/// <summary>
/// DTO for WebRTC signaling data (ICE candidates, SDP offers/answers)
/// </summary>
public class CallSignalDto
{
    public string CallId { get; set; } = string.Empty;
    public string FromUserId { get; set; } = string.Empty;
    public string ToUserId { get; set; } = string.Empty;
    public SignalType Type { get; set; }
    public string Data { get; set; } = string.Empty;
}

public enum SignalType
{
    Offer = 1,
    Answer = 2,
    IceCandidate = 3
}
