namespace TeleICU.API.DTOs.SpokeDtos
{
    public class BedsDto
    {
        public int BedId { get; set; }
        public string? BedType { get; set; }
        public string? BedNumber { get; set; }
        public string? Status { get; set; }
        public int PatientId { get; set; }
        public string? FullName { get; set; }
        public DateTime AdmitDate { get; set; }
        public int Age { get; set; }
    }
}
// BedsDtos