namespace TeleICU.API.DTOs.CallDtos
{
    public class ConsultationSummaryDto
    {
        public int SrNo { get; set; }
        public string ConsultationId { get; set; } = string.Empty;
        public int PatientId { get; set; }
        public string Patient { get; set; } = string.Empty;
        public string Specialist { get; set; } = string.Empty;
        public string CoE { get; set; } = string.Empty;
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public string Duration { get; set; } = string.Empty; // HH:MM:SS format
        public string Status { get; set; } = string.Empty;
        public bool HasPrescription { get; set; }
    }

    public class ConsultationSummaryRequest
    {
        public int? PatientId { get; set; }
        public DateTime? Date { get; set; }
        public int? CoEId { get; set; }
        public int? SpecialistId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ConsultationSummaryResponse
    {
        public IEnumerable<ConsultationSummaryDto> Data { get; set; } = new List<ConsultationSummaryDto>();
        public int TotalRecords { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
