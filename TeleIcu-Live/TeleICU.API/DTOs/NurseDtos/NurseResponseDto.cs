using TeleICU.API.DTOs.StateDtos;
using TeleICU.API.Models.AuthModels;

namespace TeleICU.API.DTOs.NurseDtos
{
    public class NurseResponseDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public UserRole RoleId { get; set; }
        public int? SpokeId { get; set; }
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
        public string Experience { get; set; }
        public int? CreatedBy { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
