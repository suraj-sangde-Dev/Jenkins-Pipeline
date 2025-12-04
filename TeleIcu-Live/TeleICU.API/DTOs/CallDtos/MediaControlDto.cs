namespace TeleICU.API.DTOs.CallDtos;

/// <summary>
/// DTO for media control (mute/unmute audio/video)
/// </summary>
public class MediaControlDto
{
    public string CallId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public MediaType Type { get; set; }
    public bool Enabled { get; set; }
}

public enum MediaType
{
    Audio = 1,
    Video = 2
}
