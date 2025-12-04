using TeleICU.API.DTOs.StateDtos;
using TeleICU.API.Models.AuthModels;

namespace TeleICU.API.DTOs.SpecialistDtos
{
    public class SpecialistResponseDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public UserRole RoleId { get; set; }
        public int? CoeId { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Gender { get; set; }
        public DateTime DOB { get; set; }
        public string Language { get; set; }
        public string SignaturePath { get; set; }
        public string ProfilePic { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public StateDto State { get; set; }
        public DistrictDto District { get; set; }
        public CityDto City { get; set; }
        public string PIN { get; set; }
        public string FacebookProfile { get; set; }
        public string TwitterProfile { get; set; }
        public string LinkedInProfile { get; set; }
        public string RegistrationNumber { get; set; }
        public string Qualification { get; set; }
        public string Speciality { get; set; }
        public string Experience { get; set; }
        public int? CreatedBy { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CoeName { get; set; } // Added for COE name display
    }
}
