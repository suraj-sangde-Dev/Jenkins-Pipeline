namespace TeleICU.API.DTOs.StateDtos
{
    public class StateDto
    {
        public int StateId { get; set; }
        public int StateCode { get; set; }
        public string StateName { get; set; }
    }
    public class DistrictDto
    {
        public int DistrictCode { get; set; }
        public string DistrictName { get; set; }
    }
    public class CityDto
    {
        public  int CityCode { get; set; }
        public string CityName { get; set; }
    }


    //public class CreateStateDto
    //{
    //    public string StateName { get; set; }
    //}
}