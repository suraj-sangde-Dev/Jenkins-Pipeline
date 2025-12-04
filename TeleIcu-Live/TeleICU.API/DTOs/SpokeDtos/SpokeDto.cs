using TeleICU.API.DTOs.StateDtos;

namespace TeleICU.API.DTOs.SpokeDtos
{
    public class SpokeDto
    {
        public int SpokeId { get; set; }
        public int? UserId { get; set; }
        public string SpokeHospitalName { get; set; }
        public int? CoeId { get; set; }
        public int StateId { get; set; }
        public string CoeName { get; set; } 
        public string StateName { get; set; } 
        public string Phone { get; set; }
        public string Email { get; set; } 
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public DistrictDto District { get; set; }
        public CityDto City { get; set; }
        public string PIN { get; set; }
        public int IcuBeds { get; set; }
        public int HduBeds { get; set; }
        public int OtherBeds { get; set; }
        public int TotalBeds { get; set; }
        public int Beds { get; set; }
        public string SpokePicturePath { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public class CreateSpokeDto
    {
        public string SpokeHospitalName { get; set; } 
        public int? CoeId { get; set; }
        public string Phone { get; set; } 
        public string Email { get; set; } 
        public string AddressLine1 { get; set; } 
        public string AddressLine2 { get; set; } 
        public string District { get; set; } 
        public string City { get; set; }
        public string PIN { get; set; }
        public int IcuBeds { get; set; }
        public int HduBeds { get; set; }
        public int OtherBeds { get; set; }
        public int Beds { get; set; }
        public IFormFile? SpokePicturePath { get; set; }
    }

    public class AssignSpokeToCoeRequest
    {
        public int CoeId { get; set; }
        public int SpokeId { get; set; }
    }

    public class AssignAdmins
    {
        public int UserId { get; set; }
    }
}