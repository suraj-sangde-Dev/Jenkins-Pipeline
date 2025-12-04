using TeleICU.API.Helpers;

namespace TeleICU.API.Repository.Interface
{
    public interface ILogRepository
    {
        Task Create(LogRequest logRequest);
    }
}
