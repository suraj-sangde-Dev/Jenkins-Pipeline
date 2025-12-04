namespace TeleICU.API.DTOs.DoctorDtos
{
    public class DoctorDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Qualification { get; set; }
        public string Speciality { get; set; }
        public string RegistrationNumber { get; set; }
        public string Contact { get; set; }
        public string SpokeHospitalName { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class AssignDoctorDto
    {
        public int UserId { get; set; }
        public int SpokeId { get; set; }
    }
}