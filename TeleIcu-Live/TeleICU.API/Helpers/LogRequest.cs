using System.ComponentModel.DataAnnotations;
using static TeleICU.API.Helpers.AppException;

namespace TeleICU.API.Helpers
{
    public class LogRequest
    {
        public string? innerException { get; set; }
        [Required]
        [EnumDataType(typeof(LogType))]
        public int? logType { get; set; }
        [Required]
        public string? message { get; set; }
        [Required]
        public string? endpoint { get; set; }
        [Required]
        public DateTime CreatedDate { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

    }
}
