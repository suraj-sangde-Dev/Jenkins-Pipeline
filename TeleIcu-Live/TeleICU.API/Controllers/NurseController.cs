using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlX.XDevAPI.Common;
using System.Security.Claims;
using TeleICU.API.DTOs.NurseDtos;
using TeleICU.API.Services.Interface;
using static TeleICU.API.Services.NurseService;

namespace TeleICU.API.Controllers
{
    [ApiController]
    [Route("api/nurse")]
    [Authorize(Roles = "super_admin,state_admin,coe_admin,spoke_admin")]
    public class NurseController : ControllerBase
    {
        private readonly INurseService _service;
        private readonly IAuthService _authService;

        public NurseController(INurseService service, IAuthService authService)
        {
            _service = service;
            _authService = authService;
        }

        [HttpGet("by-spoke/{spokeId}")]
        public async Task<IActionResult> GetBySpokeId(int spokeId)
        {
            try
            {
                var nurses = await _service.GetBySpokeId(spokeId);
                return Ok(new { data = nurses, message = "Nurses fetched successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpPost("assign")]
        public async Task<IActionResult> Assign([FromBody] AssignNurseDto dto)
        {
            try
            {
                var result = await _service.AssignNurse(dto);
                return result
                    ? Ok(new { message = "Nurse assigned successfully.", statusCode = 200 })
                    : throw new Exception($"Assignment failed");
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> Remove([FromQuery] int userId, [FromQuery] int spokeId)
        {
            try
            {
                var result = await _service.RemoveNurse(userId, spokeId);
                return result
                    ? Ok(new { message = "Nurse removed successfully.", statusCode = 200 })
                    : throw new Exception($"removal failed");
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                string role = User.FindFirstValue(ClaimTypes.Role)!;
                var nurses = await _service.GetAllNursesAsync(userId, role);
                return Ok(new { data = nurses, message = "All nurses fetched successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByNurseId(int id)
        {
            try
            {
                var nurse = await _service.GetNurse(id);
                return nurse != null
                    ? Ok(new { data = nurse, message = "Nurse fetched successfully.", statusCode = 200 })
                    : NotFound(new { message = "Nurse not found.", statusCode = 404 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }


        [HttpGet("unmapped")]
        [Authorize(Roles = "state_admin,spoke_admin")]
        public async Task<IActionResult> GetUnmappedNurses()
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var role = User.FindFirstValue(ClaimTypes.Role)!;

                IEnumerable<NurseSimpleDto> nurses;

                if (role == "state_admin")
                {
                    nurses = await _service.GetUnmappedNursesByState(userId);
                }
                else if (role == "spoke_admin")
                {
                    var user = await _authService.GetProfile(userId);
                    if (user?.StateId == null)
                        return BadRequest(new { message = "State ID not found for user.", statusCode = 400 });

                    nurses = await _service.GetUnmappedNursesByState(userId);
                }
                else
                {
                    return Unauthorized(new { message = "Unauthorized role.", statusCode = 401 });
                }

                return Ok(new { data = nurses, message = "Unmapped nurses fetched successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpGet("mapped")]
        [Authorize(Roles = "state_admin,spoke_admin")]
        public async Task<IActionResult> GetMappedNurses([FromQuery] int? spokeId = null)
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var role = User.FindFirstValue(ClaimTypes.Role)!;

                IEnumerable<NurseSimpleDto> nurses;
                int targetSpokeId;

                if (role == "state_admin")
                {
                    if (spokeId == null)
                        return BadRequest(new { message = "Spoke ID is required for state admin.", statusCode = 400 });
                    targetSpokeId = spokeId.Value;
                }
                else if (role == "spoke_admin")
                {
                    var user = await _authService.GetProfile(userId);
                    if (user?.SpokeId == null)
                        return BadRequest(new { message = "Spoke ID not found for user.", statusCode = 400 });
                    targetSpokeId = user.SpokeId.Value;
                }
                else
                {
                    return Unauthorized(new { message = "Unauthorized role.", statusCode = 401 });
                }

                nurses = await _service.GetMappedNursesBySpoke(targetSpokeId);
                return Ok(new { data = nurses, message = "Fetched MappedNurses successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpPost("map")]
        [Authorize(Roles = "state_admin,spoke_admin")]
        public async Task<IActionResult> MapNurse([FromBody] MapNurseRequest request)
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var role = User.FindFirstValue(ClaimTypes.Role)!;

                int result;
                int targetSpokeId;

                if (role == "state_admin")
                {
                    if (request.SpokeId == null)
                        return BadRequest(new { message = "Spoke ID not found for user.", statusCode = 400 });
                    targetSpokeId = request.SpokeId.Value;
                }
                else if (role == "spoke_admin")
                {
                    var user = await _authService.GetProfile(userId);
                    if (user?.SpokeId == null)
                        return BadRequest(new { message = "Spoke ID not found for user.", statusCode = 400 });
                    targetSpokeId = user.SpokeId.Value;
                }
                else
                {
                    return Unauthorized(new { message = "Unauthorized role.", statusCode = 401 });
                }

                result = await _service.MapNurseToSpoke(request.NurseId, targetSpokeId);
                return result > 0 ? Ok(new { data = result, message = "Nurse mapped successfully" }) : throw new Exception($"Mapping failed");
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpDelete("unmap/{nurseId}")]
        [Authorize(Roles = "state_admin,spoke_admin")]
        public async Task<IActionResult> UnmapNurse(int nurseId)
        {
            try
            {
                var result = await _service.UnmapNurse(nurseId);
                return result > 0 ? Ok(new { data = result, message = "Nurse unmapped successfully" }) : throw new Exception($"unmapping failed");
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        //prakhar on 24 oct

        [HttpGet("caseId")]
        public async Task<IActionResult> GetNurseByCaseId(int caseId)
        {
            try
            {
                var nurse = await _service.GetNurseByCaseId(caseId);
                return nurse != null
                    ? Ok(new { data = nurse, message = "Nurse details fetched successfully.", statusCode = 200 })
                    : NotFound(new { message = "Nurse not found.", statusCode = 404 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }
    }
}
