using System.Text.Json.Serialization;

namespace TeleICU.API.DTOs.StateDtos
{
    public class StateAdminResponseDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public int RoleId { get; set; }
        public int? StateId { get; set; }
        public int? CoeId { get; set; }
        public int? SpokeId { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }
        public DateTime Dob { get; set; }
        public string Language { get; set; }
        public string? SignaturePath { get; set; }
        public string? ProfilePic { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public StateDto State { get; set; }
        public DistrictDto District { get; set; }
        public CityDto City { get; set; }
        public string Pin { get; set; }
        public string? FacebookProfile { get; set; }
        public string? TwitterProfile { get; set; }
        public string? LinkedInProfile { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? Qualification { get; set; }
        public string? Speciality { get; set; }
        public string? Experience { get; set; }
        public int CreatedBy { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        // extra nullables
         [JsonIgnore]
        public string? StateName { get; set; }
        [JsonIgnore]
        public string? StateCode { get; set; }
        [JsonIgnore]
        public string? CoeName { get; set; }
        [JsonIgnore]
        public string? SpokeHospitalName { get; set; }
        [JsonIgnore]
        public List<StateDtos>? States { get; set; }
        [JsonIgnore]
        public List<DistrictDtos>? Districts { get; set; }
        [JsonIgnore]
        public List<CityDtos>? Cities { get; set; }
    }

    public class StateDtos
    {
        public int StateId { get; set; }
        public string StateName { get; set; }
        public string StateCode { get; set; }
    }

    public class DistrictDtos
    {
        public int DistrictId { get; set; }
        public string DistrictName { get; set; }
        public string DistrictCode { get; set; }
    }

    public class CityDtos
    {
        public int CityId { get; set; }
        public string CityName { get; set; }
        public string CityCode { get; set; }
    }

}
