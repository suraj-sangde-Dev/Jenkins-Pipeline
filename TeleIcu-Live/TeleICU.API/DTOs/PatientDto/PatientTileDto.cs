using System;

namespace TeleICU.API.DTOs.PatientDto
{
    public class PatientTileDto
    {
        public int PatientId { get; set; }
		public int? CaseId { get; set; }
        public string PatientName { get; set; } = string.Empty;
		public string SpokeHospitalName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public int AgeYears { get; set; }
        public int AgeMonths { get; set; }
        public string BloodGroup { get; set; } = string.Empty;
        public string BedNumber { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string AdmittedOn { get; set; } = string.Empty; // formatted dd-MM-YYYY
        public string UpdatedVital { get; set; } = string.Empty; // formatted dd-MM-YYYY

        // Latest vitals (nullable when not recorded yet)
        public decimal? Temperature { get; set; }
        public int? RespiratoryRate { get; set; }
        public decimal? OxygenSaturation { get; set; }
        public int? BloodPressureSYS { get; set; }
        public int? BloodPressureDIA { get; set; }
        public int? HeartRate { get; set; }
        public int? lastConsultedSpecialist { get; set; }
        public int? CreatedBy { get; set; }
    }
}


