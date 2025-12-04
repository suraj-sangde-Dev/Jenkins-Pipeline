using Dapper;
using TeleICU.API.Helpers;
using TeleICU.API.Repository.Interface;

namespace TeleICU.API.Repository
{
    public class LogRepository : ILogRepository
    {
        private readonly DapperContext _context;

        public LogRepository(DapperContext context)
        {
            _context = context;
        }
        public async Task Create(LogRequest logRequest)
        {
            using var connection = _context.CreateConnection();
            var sql = """
            INSERT INTO log (logType, message, endpoint, CreatedDate)
            VALUES (@logType, @message, @endpoint, @CreatedDate)
        """;
            await connection.ExecuteAsync(sql, logRequest);
        }
    }
}
