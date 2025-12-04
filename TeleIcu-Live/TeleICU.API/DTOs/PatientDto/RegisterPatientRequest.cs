using System.ComponentModel.DataAnnotations;
namespace TeleICU.API.DTOs.PatientDto

{
    public class RegisterPatientRequest
    {
        [Required]
        public int? BedId { get; set; }
        [Required]
        public int DoctorId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public DateTime DOB { get; set; }
        public string Gender { get; set; } = string.Empty; // Male, Female, Other
        public decimal WeightInKg { get; set; }
        public string BloodGroup { get; set; } = string.Empty;
       // public string DoctorName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
        public string State { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
    }

}
