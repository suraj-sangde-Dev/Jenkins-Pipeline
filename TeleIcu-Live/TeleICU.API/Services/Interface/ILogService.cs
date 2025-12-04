using TeleICU.API.Helpers;

namespace TeleICU.API.Services.Interface
{
    public interface ILogService
    {
        Task Create(LogRequest logRequest);
    }
}
