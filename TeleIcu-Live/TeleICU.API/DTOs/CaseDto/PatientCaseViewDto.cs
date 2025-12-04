using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TeleICU.API.DTOs.CaseDto
{
    public class PatientCaseViewDto
    {
        public int CaseId { get; set; }
        public int PatientId { get; set; }

        // Header
        public string PatientName { get; set; } = string.Empty;
        public int AgeYears { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string BedNumber { get; set; } = string.Empty;
        public string ICU { get; set; } = string.Empty; // Spoke hospital name
        public DateTime AdmitDateTime { get; set; }
        [JsonIgnore]
        public string SpokeState { get; set; } = string.Empty;
        [JsonIgnore]
        public string SpokeDistrict { get; set; } = string.Empty;
        [JsonIgnore]
        public string SpokeCity { get; set; } = string.Empty;
        [JsonIgnore]
        public string SpokePIN { get; set; } = string.Empty;
        [JsonIgnore]
        public string SpokeAddressLine1 { get; set; } = string.Empty;
        [JsonIgnore]
        public string? SpokeAddressLine2 { get; set; }
        public string Address { get; set; } = string.Empty;

        // Case summary
        public string ChiefComplaint { get; set; } = string.Empty;
        public string? ComplaintDescription { get; set; }
        public string? Query { get; set; }
        public string? Examination { get; set; }
        public string? Advice { get; set; }
        public string? SpecialistName { get; set; }
        public string? SignaturePath { get; set; }
        public string? Speciality { get; set; }
        public string? ConsultationId { get; set; }
        public DateTime VitalUpdate { get; set; }
        public int PrescribedBy { get; set; }


        // Histories
        public string MedicalHistory { get; set; } = string.Empty;
        public string FamilyHistory { get; set; } = string.Empty;
        public string Allergies { get; set; } = string.Empty;

        // Sections
        public VitalDto? LatestVitals { get; set; }
        public NurseDetailsDto? NurseDetails { get; set; }
        public List<PreAdmitMedicationDto> PreAdmitMedications { get; set; } = new();
        public List<CaseHealthRecordItemDto> HealthRecords { get; set; } = new();
        public List<PatientMedicationItemDto> PrescribedMedications { get; set; } = new();
    }

    public class CaseHistoryItemDto
    {
        public string HistoryType { get; set; } = string.Empty; // Medical, Family, Allergy
        public string ConditionName { get; set; } = string.Empty;
    }

    public class CaseHealthRecordItemDto
    {
        public int CaseHealthRecordId { get; set; }
        public string? RecordType { get; set; }
        public string? FilePath { get; set; }
        public DateTime UploadedDate { get; set; }
    }

    public class PatientMedicationItemDto
    {
        public int MedicationId { get; set; }
        public string Medicine { get; set; } = string.Empty;
        public string? Frequency { get; set; }
        public string? Dose { get; set; }
        public string? Type { get; set; }
        public int? DurationValue { get; set; }
        public string? DurationType { get; set; }
        public string? PrescribedBy { get; set; }
        public string? ConsultationId { get; set; }
        public DateTime? DateTime { get; set; }
        public string Status { get; set; } = "Ongoing"; // Ongoing/Discontinued
    }

    public class CasePreAdmitMedicationResponseDto
    {
        public int CasePreAdmitMedicationId { get; set; }
        public int CaseId { get; set; }
        public string Medicine { get; set; } = string.Empty;
        public string? Frequency { get; set; }
        public string? Dose { get; set; }
        public string? Type { get; set; }
        public int? DurationValue { get; set; }
        public string? DurationType { get; set; }
        public string PrescribedBy { get; set; } = string.Empty; // User name
        public string? ConsultationId { get; set; }
        public DateTime PrescribedDateTime { get; set; }
        public string Status { get; set; } = "Ongoing";
    }
}


