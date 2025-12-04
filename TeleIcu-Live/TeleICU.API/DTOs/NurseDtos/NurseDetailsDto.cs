namespace TeleICU.API.DTOs.NurseDtos
{
    public class NurseDetailsDto
    {
        public int UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? SpokeHospitalName { get; set; }
        public int RoleId { get; set; }
    }
}
