namespace TeleICU.API.DTOs.CallDtos;

/// <summary>
/// DTO for call response (accept/reject)
/// </summary>
public class CallResponseDto
{
    public string CallId { get; set; } = string.Empty;
    public string ResponderId { get; set; } = string.Empty;
    public bool Accepted { get; set; }
}
