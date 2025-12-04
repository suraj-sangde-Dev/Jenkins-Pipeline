using TeleICU.API.DTOs.StateDtos;

namespace TeleICU.API.DTOs.CoeDtos
{
    public class CoeDto
    {
        public int CoeId { get; set; }
        public int? UserId { get; set; }
        public string CoeName { get; set; }
        public string CoeCode { get; set; }
        public int StateId { get; set; }
        public string StateName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public DistrictDto District { get; set; }
        public CityDto City { get; set; }
        public string PIN { get; set; }
        public string? CoePicture { get; set; }
        public int CreatedBy { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public class CreateCoeDto
    {
        public string CoeName { get; set; }
        public string CoeCode { get; set; }
       public int? StateId { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string District { get; set; }
        public string City { get; set; }
        public string PIN { get; set; }
        public IFormFile? CoePicture { get; set; }
    }
}