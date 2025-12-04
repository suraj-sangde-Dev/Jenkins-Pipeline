using System.ComponentModel.DataAnnotations;

namespace TeleICU.API.DTOs.AuthDtos
{
    public class UpdateUserRequest
    {
        public string? Phone { get; set; }
        public string? Title { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? Gender { get; set; }
        public DateTime? DOB { get; set; }
        public string? Language { get; set; }
        public IFormFile? SignatureFile { get; set; }
        public IFormFile? ProfilePic { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? State { get; set; }
        public string? District { get; set; }
        public string? City { get; set; }
        public string? PIN { get; set; }
        public string? FacebookProfile { get; set; }
        public string? TwitterProfile { get; set; }
        public string? LinkedInProfile { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? Qualification { get; set; }
        public string? Speciality { get; set; }
        public string? Experience { get; set; }
        public bool? IsActive { get; set; }
    }
}
