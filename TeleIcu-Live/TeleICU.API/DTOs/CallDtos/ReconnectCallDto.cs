namespace TeleICU.API.DTOs.CallDtos;

/// <summary>
/// DTO for reconnecting to an existing encounter
/// </summary>
public class ReconnectCallDto
{
    public string EncounterId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}
