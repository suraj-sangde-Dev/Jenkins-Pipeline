using System;

namespace TeleICU.API.DTOs.PatientDto
{
    public class VitalTrendDto
    {
        public int VitalId { get; set; }
        public int PatientId { get; set; }
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
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public class VitalTrendRequest
    {
        public int PatientId { get; set; }
        public string TimeRange { get; set; } = "24"; // Default to 24 hours
    }

    public class VitalTrendResponse
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string TimeRange { get; set; } = string.Empty;
        public List<VitalTrendDto> Vitals { get; set; } = new List<VitalTrendDto>();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
