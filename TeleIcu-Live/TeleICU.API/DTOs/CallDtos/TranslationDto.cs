namespace TeleICU.API.DTOs.CallDtos;

/// <summary>
/// DTO for real-time translation during calls
/// </summary>
public class TranslationDto
{
    public string CallId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string SourceLanguage { get; set; } = string.Empty;
    public string TargetLanguage { get; set; } = string.Empty;
    public string OriginalText { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;
}
