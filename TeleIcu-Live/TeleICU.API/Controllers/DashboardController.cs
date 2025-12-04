using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _service;

        public DashboardController(IDashboardService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
            {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                string role = User.FindFirstValue(ClaimTypes.Role)!;

                var dashboard = await _service.GetDashboardCounts(userId, role);

                return Ok(new
                {
                    data = dashboard,
                    message = "Dashboard data fetched successfully.",
                    statusCode = 200
                });
            }
            catch (Exception ex)
            {
                //return StatusCode(500, new
                //{
                //    message = ex.Message,
                //    statusCode = 500
                //});
                throw new Exception($"{ex.Message}", ex);
            }
        }
    }
}
