namespace TeleICU.API.DTOs.ReportsDto
{
    public class ReportsDto
    {
        public string? ConsultationId { get; set; }
        public int PatientId { get; set; }
        public string? PatientName { get; set; }
        public string? NurseDoctor { get; set; }
        public string? Specialist { get; set; }
        public string? SpokeHospitalName { get; set; }
        public string? CoeName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool Status { get; set; }
        public string? LatestFilePath { get; set;}
    }

    public class ConsultationRequest
    {
        public int? PatientId { get; set; }
        public int? SpokeId { get; set; }
    }


}
