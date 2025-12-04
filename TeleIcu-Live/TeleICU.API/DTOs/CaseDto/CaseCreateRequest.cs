using System.Text.Json.Serialization;

namespace TeleICU.API.DTOs.CaseDto
{
    // Case
    public class CreateCaseDto
    {
        public int? CaseId { get; set; }
        public int PatientId { get; set; }
        public string ChiefComplaint { get; set; } = string.Empty;
        public string? ComplaintDescription { get; set; }
     //   public string? Query { get; set; }
    }

    public class CaseHistoryDto
    {
        public int CaseId { get; set; }
        public string? Medical { get; set; }
        public string? Family { get; set; }
        public string? Allergies { get; set; }
    }

    // Pre-Admission Medication
    public class PreAdmitMedicationDto
    {
        public int CaseId { get; set; }
        public string Medicine { get; set; } = string.Empty;
        public string? Frequency { get; set; }
        public string? Dose { get; set; }
        public string? Type { get; set; }
        public int? DurationValue { get; set; }
        public string? DurationType { get; set; }
        public int PrescribedBy { get; set; }
        public string? ConsultationId { get; set; }
        public string Status { get; set; } = "Ongoing";
    }

    // Vitals
    public class VitalDto
    {
        public int CaseId { get; set; }
        public decimal Temperature { get; set; }
        public int RespiratoryRate { get; set; }
        public decimal OxygenSaturation { get; set; }
        public int BloodPressureDIA { get; set; }
        public int BloodPressureSYS { get; set; }
        public int HeartRate { get; set; }
        public int PulseRate { get; set; }
        public decimal BloodGlucose { get; set; }
        public decimal FIO2 { get; set; }
        public decimal ETCO2 { get; set; }
        public decimal BMI { get; set; }
        public decimal RightAtrialPressure { get; set; }
        [JsonIgnore]
        public DateTime CreatedDate { get; set; }
    }

    // nurse details dto

    public class NurseDetailsDto
    {
        public string NurseName { get; set; } = string.Empty;
        public string NurseAddress { get; set; } = string.Empty;
        public string StateName { get; set; } = string.Empty;
        public string DistrictName { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;
        public string PIN { get; set; } = string.Empty;
        public string SpokeHospitalName { get; set; } = string.Empty;
        public string Speciality { get; set; } = string.Empty;
    }


    // Health Record Upload
    public class CaseHealthRecordUploadDto
    {
        public int CaseId { get; set; }
        // RecordTypes must align by index with File list. For single file, provide one item.
        public List<string> RecordTypes { get; set; } = new List<string>();
        public List<IFormFile> File { get; set; } = new List<IFormFile>();
    }

    public class CaseQueryDto
    {
        public int CaseId { get; set; }
        public string Query { get; set; } = string.Empty;
    }
}
