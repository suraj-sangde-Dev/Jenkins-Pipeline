using TeleICU.API.DTOs.StateDtos;

namespace TeleICU.API.DTOs.PatientDto
{
    public class PatientDetailDto
    {
        public int PatientId { get; set; }
        public int SpokeId { get; set; }
        public int? CaseId { get; set; }
        public int? BedId { get; set; }
        public string? BedNumber { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime DOB { get; set; }
        public int AgeYears { get; set; }
        public int AgeMonths { get; set; }
        public string Gender { get; set; } = string.Empty;
        public decimal WeightInKg { get; set; }
        public string BloodGroup { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
		public StateDto State { get; set; }
		public DistrictDto District { get; set; }
		public CityDto City { get; set; }
        public string Pin { get; set; } = string.Empty;
        public DateTime AdmitDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int CreatedBy { get; set; }
    }
}
