using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeleICU.API.DTOs.DoctorDtos;
using TeleICU.API.Services.Interface;
using static TeleICU.API.Services.DoctorService;
using static TeleICU.API.Services.NurseService;

namespace TeleICU.API.Controllers
{
    [ApiController]
    [Route("api/doctor")]
    [Authorize(Roles = "super_admin,state_admin,coe_admin,spoke_admin")]
    public class DoctorController : ControllerBase
    {
        private readonly IDoctorService _service;
        private readonly IAuthService _authService;

        public DoctorController(IDoctorService service, IAuthService authService)
        {
            _service = service;
            _authService = authService;
        }

        [HttpGet("by-spoke/{spokeId}")]
        public async Task<IActionResult> GetBySpokeId(int spokeId)
        {
            try
            {
                var doctors = await _service.GetBySpokeId(spokeId);
                return Ok(new { data = doctors, message = "Doctors fetched successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpPost("assign")]
        public async Task<IActionResult> Assign([FromBody] AssignDoctorDto dto)
        {
            try
            {
                var result = await _service.AssignDoctor(dto);
                return result
                    ? Ok(new { message = "Doctor assigned successfully.", statusCode = 200 })
                    : throw new Exception($"Assignment  failed");
            }
            catch (Exception ex)
            {
                //return StatusCode(500, new { message = ex.Message, statusCode = 500 });
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> Remove([FromQuery] int userId, [FromQuery] int spokeId)
        {
            try
            {
                var result = await _service.RemoveDoctor(userId, spokeId);
                return result
                    ? Ok(new { message = "Doctor removed successfully.", statusCode = 200 })
                    : throw new Exception($"Removal failed");
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
                var doctors = await _service.GetAllDoctors(userId, role);
                return Ok(new { data = doctors, message = "Doctors fetched successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                    throw new Exception($"{ex.Message}", ex);
                }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByDoctorId(int id)
        {
            try
            {
                var doctor = await _service.GetDoctor(id);
                return doctor != null
                    ? Ok(new { data = doctor, message = "Doctor fetched successfully.", statusCode = 200 })
                    : NotFound(new { message = "Doctor not found.", statusCode = 404 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpGet("unmapped")]
        [Authorize(Roles = "state_admin,spoke_admin")]
        public async Task<IActionResult> GetUnmappedDoctors()
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var role = User.FindFirstValue(ClaimTypes.Role)!;
                
                IEnumerable<DoctorSimpleDto> doctors;
                
                if (role == "state_admin")
                {
                    doctors = await _service.GetUnmappedDoctorsByState(userId);
                }
                else if (role == "spoke_admin")
                {
                    var user = await _authService.GetProfile(userId);
                    if (user?.StateId == null)
                        return BadRequest(new { message = "State ID not found for user.", statusCode = 400 });
                    
                    doctors = await _service.GetUnmappedDoctorsByState(userId);
                }
                else
                {
                    return BadRequest(new { message = "Unauthorized role.", statusCode = 400 });
                }
                
                return Ok(new { data = doctors, message = "Unmapped doctors fetched successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpGet("mapped")]
        [Authorize(Roles = "state_admin,spoke_admin")]
        public async Task<IActionResult> GetMappedDoctors([FromQuery] int? spokeId = null)
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var role = User.FindFirstValue(ClaimTypes.Role)!;
                
                IEnumerable<DoctorSimpleDto> doctors;
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
                        return NotFound(new { message = "Spoke ID not found for user.", statusCode = 404 });
                    targetSpokeId = user.SpokeId.Value;
                }
                else
                {
                    return Unauthorized(new { message = "Unauthorized role.", statusCode = 401 });
                }
                
                doctors = await _service.GetMappedDoctorsBySpoke(targetSpokeId);
                return Ok(new { data = doctors, message = "Fetched MappedDoctors successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpPost("map")]
        [Authorize(Roles = "state_admin,spoke_admin")]
        public async Task<IActionResult> MapDoctor([FromBody] MapDoctorRequest request)
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
                        return BadRequest(new { message = "Spoke ID is required for state admin.", statusCode = 400 });
                    targetSpokeId = request.SpokeId.Value;
                }
                else if (role == "spoke_admin")
                {
                    var user = await _authService.GetProfile(userId);
                    if (user?.SpokeId == null)
                        return BadRequest(new { message = "Spoke ID is required for state admin.", statusCode = 400 });
                    targetSpokeId = user.SpokeId.Value;
                }
                else
                {
                    return Unauthorized(new { message = "Unauthorized role.", statusCode = 401 });
                }
                
                result = await _service.MapDoctorToSpoke(request.DoctorId, targetSpokeId);
                return result > 0 ? Ok(new { data = result, message = "Doctor mapped successfully" }) : BadRequest("Mapping failed");
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpDelete("unmap/{doctorId}")]
        [Authorize(Roles = "state_admin,spoke_admin")]
        public async Task<IActionResult> UnmapDoctor(int doctorId)
        {
            try
            {
                var result = await _service.UnmapDoctor(doctorId);
                return result > 0 ? Ok(new { data = result, message = "Doctor unmapped successfully" }) : BadRequest("Unmapping failed");
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }
    }
}
