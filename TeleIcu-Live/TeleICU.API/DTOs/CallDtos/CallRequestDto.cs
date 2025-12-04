namespace TeleICU.API.DTOs.CallDtos;

/// <summary>
/// DTO for initiating a call request
/// </summary>
public class CallRequestDto
{
    public string CallerId { get; set; } = string.Empty;
    public string CallerName { get; set; } = string.Empty;
    public string CalleeId { get; set; } = string.Empty;
    public string CalleeName { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public CallType CallType { get; set; }
}

public enum CallType
{
    Audio = 1,
    Video = 2
}
