using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TeleICU.API.DTOs.ReportsDto;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Controllers
{
    [Route("api/reports")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly IReportsService _reportsService;
        public ReportsController(IReportsService reportsService)
        {
            _reportsService = reportsService;
        }

        [HttpPost("getconsultationsummary")]
        public async Task<IActionResult> GetConsultations([FromBody] ConsultationRequest request)
        {
            try
            {
                var result = await _reportsService.GetConsultationsAsync(request);
                return Ok(new
                {
                    data = result,
                    message = "Summary data fetched successfully.",
                    statusCode = 200
                });
            }
            catch(Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }
    }
}
