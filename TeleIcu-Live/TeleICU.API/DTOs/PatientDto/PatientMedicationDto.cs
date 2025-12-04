using System;

namespace TeleICU.API.DTOs.PatientDto
{
    public class PatientMedicationDto
    {
        public int MedicationId { get; set; } // Maps to CasePreAdmitMedicationId
        public int CaseId { get; set; }
        public string Medicine { get; set; } = string.Empty;
        public string? Frequency { get; set; }
        public string? Dose { get; set; }
        public string? Type { get; set; }
        public int? DurationValue { get; set; }
        public string? DurationType { get; set; }
        public int PrescribedBy { get; set; }
        public string? ConsultationId { get; set; }
        public string? CallId { get; set; }
        public DateTime? DateTime { get; set; } // Maps to PrescribedDateTime
        public string Status { get; set; } = "Ongoing";
        public bool IsSynced { get; set; } = false;
        public DateTime? SyncedTime { get; set; }
    }

    public class PatientMedicationQuery
    {
        public int PatientId { get; set; }
      //  public bool IncludeDiscontinued { get; set; } = false;
    }
}
