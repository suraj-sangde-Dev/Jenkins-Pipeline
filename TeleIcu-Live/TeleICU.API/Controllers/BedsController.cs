using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Controllers
{
    [Route("api/beds")]
    [ApiController]
    public class BedsController : ControllerBase
    {
        private readonly IBedsService _service;
        private readonly ISpokeService _spokeservice;

        public BedsController(IBedsService service, ISpokeService spokeservice)
        {
            _service = service;
            _spokeservice = spokeservice;
        }
        [HttpGet("bedslist/{spokeId}")]
        public async Task<IActionResult> GetBeds(int spokeId)
        {
            try
            {
                //var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                //string role = User.FindFirstValue(ClaimTypes.Role)!;

                //var spokes = await _spokeservice.GetSpokesByRole(userId, role);

                //if (spokeId == 0 && spokes.Any())
                //{
                //    spokeId = spokes.First().SpokeId;
                //}

                var bedlist = await _service.GetBeds(spokeId);
                if (bedlist == null || !bedlist.Any())  // <-- check for null or empty
                {
                    return Ok(new { data = bedlist, message = "Beds are unavailable.", statusCode = 404 });
                }
                return Ok(new { data = bedlist, message = "Beds fetched successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }
    }
}
