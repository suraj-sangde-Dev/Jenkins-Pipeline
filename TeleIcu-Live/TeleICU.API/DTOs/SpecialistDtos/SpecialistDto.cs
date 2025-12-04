namespace TeleICU.API.DTOs.SpecialistDtos
{
    public class SpecialistDto
    {
        public int SpecialistId { get; set; }
        public int UserId { get; set; }
        public int CoeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class AllSpecialistDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Qualification { get; set; }
        public string Speciality { get; set; }
        public string CoeName { get; set; }
        public string RegistrationNumber { get; set; }
        public string Phone { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class AssignSpecialistDto
    {
        public int UserId { get; set; }
        public int CoeId { get; set; }
    }

    public class AvailableSpecialistDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Speciality { get; set; } = string.Empty;
        public string CoeName { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public bool IsOnCall { get; set; }
    }
}