using System.ComponentModel.DataAnnotations;

namespace TeleICU.API.Models.CoeModels
{
    public class CenterOfExcellenceModel
    {
        public int CoeId { get; set; }
        public int? UserId { get; set; }
        [Required]
        public string CoeName { get; set; }
        [Required]
        public string CoeCode { get; set; }
        public int StateId { get; set; }
        public string StateName { get; set; }
        public string? CoePicture { get; set; }
        [Required]
        public string Phone { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        [Required]
        public string District { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string PIN { get; set; }
        public int CreatedBy { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        
        // Additional properties for district and city details
        public int? DistrictCode { get; set; }
        public string? DistrictName { get; set; }
        public int? CityCode { get; set; }
        public string? CityName { get; set; }
    }
}