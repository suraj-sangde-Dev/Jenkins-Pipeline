namespace TeleICU.API.DTOs.CallDtos;

public class CallPrescriptionDataDto
{
    public string CallId { get; set; } = string.Empty;
    public string ConsultationId { get; set; } = string.Empty;
    public string SpecialistName { get; set; } = string.Empty;
    public string SpecialistTitle { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderAddress { get; set; } = string.Empty;
    public DateTime ConsultationDateTime { get; set; }
}


