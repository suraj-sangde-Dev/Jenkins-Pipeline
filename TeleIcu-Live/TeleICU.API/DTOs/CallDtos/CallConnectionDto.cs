namespace TeleICU.API.DTOs.CallDtos;

/// <summary>
/// DTO for establishing call connection (maps to inVC conn_obj)
/// </summary>
public class CallConnectionDto
{
    public string ServerURL { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string AppName { get; set; } = "TeleICU";
    public string SelfName { get; set; } = string.Empty;
    public string EncounterUid { get; set; } = string.Empty;
    public string Uid { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
}
