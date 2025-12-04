using TeleICU.API.Helpers;
using TeleICU.API.Repository.Interface;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Services
{
    public class LogService : ILogService 
    {
        private ILogRepository _logRepository;
        public LogService(ILogRepository logService)
        {

            this._logRepository = logService;
        }
        public async Task Create(LogRequest logRequest)
        {
            await _logRepository.Create(logRequest);
            Console.WriteLine("log called");
        }
    }
}
