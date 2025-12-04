namespace TeleICU.API.Models.Beds
{
    public class BedModel
    {
        public int BedId { get; set; }
        public string BedNumber { get; set; } = string.Empty;
        public string BedType { get; set; } = string.Empty; // ICU/HDU/General/Other
    }

}
