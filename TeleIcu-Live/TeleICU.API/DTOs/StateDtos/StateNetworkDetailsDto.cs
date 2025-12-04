namespace TeleICU.API.DTOs.StateDtos
{
    public class StateNetworkDetailsDto
    {
        public int StateId { get; set; }
        public string StateName { get; set; }
        public string AdminName { get; set; }
        public int CoeCount { get; set; }
        public int SpokeCount { get; set; }
    }
}