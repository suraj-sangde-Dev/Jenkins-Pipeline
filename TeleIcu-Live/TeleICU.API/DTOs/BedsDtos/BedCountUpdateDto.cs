namespace TeleICU.API.DTOs.BedsDtos
{
    public class BedCountUpdateDto
    {
        public int IcuBeds { get; set; }
        public int HduBeds { get; set; }
        public int OtherBeds { get; set; }
    }

}
