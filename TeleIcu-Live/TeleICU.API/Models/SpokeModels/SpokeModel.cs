namespace TeleICU.API.Models.SpokeModels
{
    public class SpokeModel
    {
        public int SpokeId { get; set; }
        public string SpokeHospitalName { get; set; }
        public int? CoeId { get; set; }
        public int StateId { get; set; }
        public string CoeName { get; set; } 
        public string StateName { get; set; } 
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
        public int TotalBeds { get; set; }
        public int Beds { get; set; }
        public string SpokePicturePath { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        
        // Additional properties for district and city details
        public int? DistrictCode { get; set; }
        public string? DistrictName { get; set; }
        public int? CityCode { get; set; }
        public string? CityName { get; set; }

        public int? UserId { get; set; } // Spoke admin user id (if assigned)
    }
}