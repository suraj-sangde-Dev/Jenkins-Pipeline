using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TeleICU.API.Helpers;
using TeleICU.API.Services;
using TeleICU.API.Services.Interface;
using static TeleICU.API.Helpers.AppException;

namespace TeleICU.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogsController : ControllerBase
    {
        private readonly ILogService _logService;

        public LogsController(ILogService logService)
        {
            _logService = logService;
        }
        [HttpPost("test-error")]
        public Task<IActionResult> TestError()
        {
            throw new Exception("Manually triggered exception for testing log service");
        }
    }
}
