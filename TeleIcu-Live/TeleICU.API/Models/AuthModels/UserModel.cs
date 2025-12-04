namespace TeleICU.API.Models.AuthModels
{
    public class UserModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public UserRole RoleId { get; set; }
        public int? StateId { get; set; }
        public int? CoeId { get; set; }
        public int? SpokeId { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }
        public DateTime DOB { get; set; }
        public string Language { get; set; }
        public string SignaturePath { get; set; }
        public string ProfilePic { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string State { get; set; }
        public string District { get; set; }
        public string City { get; set; }
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
        public DateTime UpdatedDate { get; set; }


        public string? StateName { get; set; }
        public string? StateCode { get; set; }
        public string? CoeName { get; set; }
        public string? SpokeHospitalName { get; set; }

    }

    public enum UserRole
    {
        super_admin = 1,
        state_admin = 2,
        coe_admin = 3,
        spoke_admin = 4,
        specialist = 5,
        doctor = 6,
        nurse = 7
    }
}