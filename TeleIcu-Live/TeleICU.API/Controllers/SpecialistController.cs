using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeleICU.API.DTOs.SpecialistDtos;
using TeleICU.API.Services;
using TeleICU.API.Services.Interface;
using static TeleICU.API.Services.SpecialistService;

namespace TeleICU.API.Controllers
{
    [ApiController]
    [Route("api/specialist")]
    [Authorize(Roles = "super_admin,state_admin,coe_admin,nurse")]
    public class SpecialistController : ControllerBase
    {
        private readonly ISpecialistService _service;
        private readonly IAuthService _authService;

        public SpecialistController(ISpecialistService service, IAuthService authService)
        {
            _service = service;
            _authService = authService;
        }

        [HttpGet("by-coe/{coeId}")]
        public async Task<IActionResult> GetByCoeId(int coeId)
        {
            try
            {
                var specialists = await _service.GetByCoeId(coeId);
                return Ok(new { data = specialists, message = "Specialists fetched successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBySpecialistId(int id)
        {
            try
            {
                var specialist = await _service.GetSpecialist(id);
                return specialist != null
                    ? Ok(new { data = specialist, message = "Specialist fetched successfully.", statusCode = 200 })
                    : NotFound(new { message = "Specialist not found.", statusCode = 404 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpPost("assign")]
        public async Task<IActionResult> Assign([FromBody] AssignSpecialistDto dto)
        {
            try
            {
                var result = await _service.AssignSpecialist(dto);
                return result
                    ? Ok(new { message = "Specialist assigned successfully.", statusCode = 200 })
                    : throw new Exception($"Assignment failed");
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> Remove([FromQuery] int userId, [FromQuery] int coeId)
        {
            try
            {
                var result = await _service.RemoveSpecialist(userId, coeId);
                return result
                    ? Ok(new { message = "Specialist removed successfully.", statusCode = 200 })
                    : throw new Exception($"Removal failed");
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                string role = User.FindFirstValue(ClaimTypes.Role)!;
                
                var specialists = await _service.GetAllSpecialists(userId, role);
                return Ok(new { data = specialists, message = "All specialists fetched successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }


        [HttpGet("unmapped")]
        [Authorize(Roles = "state_admin,coe_admin")]
        public async Task<IActionResult> GetUnmappedSpecialists()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var role = User.FindFirstValue(ClaimTypes.Role)!;
                
                IEnumerable<SpecialistSimpleDto> specialists;
                
                if (role == "state_admin")
                {
                    specialists = await _service.GetUnmappedSpecialistsByState(userId);
                }
                else if (role == "coe_admin")
                {
                    var user = await _authService.GetProfile(userId);
                    if (user?.CoeId == null)
                        return NotFound(new { message = "COE ID not found for user.", statusCode = 404 });
                    
                    // For COE admin, get unmapped specialists in their state (same as state admin logic)
                    specialists = await _service.GetUnmappedSpecialistsByState(userId);
                }
                else
                {
                    return Forbid();
                }

                return Ok(new { data = specialists, message = "Unmapped specialists fetched successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpGet("mapped")]
        [Authorize(Roles = "state_admin,coe_admin")]
        public async Task<IActionResult> GetMappedSpecialists([FromQuery] int? coeId = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var role = User.FindFirstValue(ClaimTypes.Role)!;
                
                IEnumerable<SpecialistSimpleDto> specialists;
                
                if (role == "state_admin")
                {
                    if (coeId == null)
                        return BadRequest(new { message = "COE ID is required for state admin.", statusCode = 400 });
                        
                    specialists = await _service.GetMappedSpecialistsByCoe(coeId.Value);
                }
                else if (role == "coe_admin")
                {
                    var user = await _authService.GetProfile(userId);
                    if (user?.CoeId == null)
                        return BadRequest(new { message = "COE ID not found for user.", statusCode = 400 });

                    // For COE admin, get mapped specialists for their COE
                    specialists = await _service.GetMappedSpecialistsByCoe(user.CoeId.Value);
                }
                else
                {
                    return Forbid();
                }

                return Ok(new { data = specialists, message = "Mapped specialists fetched successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpPost("map")]
        [Authorize(Roles = "state_admin,coe_admin")]
        public async Task<IActionResult> MapSpecialist([FromBody] MapSpecialistRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var role = User.FindFirstValue(ClaimTypes.Role)!;
                
                int result;
                
                if (role == "state_admin")
                {
                    if (request.CoeId == null)
                        throw new Exception($"COE ID is required for state admin");
                    
                    result = await _service.MapSpecialistToCoe(request.SpecialistId, request.CoeId.Value);
                }
                else if (role == "coe_admin")
                {
                    var user = await _authService.GetProfile(userId);
                    if (user?.CoeId == null)
                        return NotFound(new { message = "COE ID not found for user.", statusCode = 404 });

                    // For COE admin, map specialist to their COE
                    result = await _service.MapSpecialistToCoe(request.SpecialistId, user.CoeId.Value);
                }
                else
                {
                    return Forbid();
                }
                
                return result > 0 
                    ? Ok(new { data = result, message = "Specialist mapped successfully.", statusCode = 200 })
                    : throw new Exception($"Mapping failed");
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpDelete("unmap/{specialistId}")]
        [Authorize(Roles = "state_admin,coe_admin")]
        public async Task<IActionResult> UnmapSpecialist(int specialistId, [FromQuery] int? coeId = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var role = User.FindFirstValue(ClaimTypes.Role)!;
                
                int result;
                
                if (role == "state_admin")
                {
                    result = await _service.UnmapSpecialist(specialistId);
                }
                else if (role == "coe_admin")
                {
                    var user = await _authService.GetProfile(userId);
                    if (user?.CoeId == null)
                        return NotFound(new { message = "COE ID not found for user.", statusCode = 404 });

                    // For COE admin, unmap specialist (same as state admin logic)
                    result = await _service.UnmapSpecialist(specialistId);
                }
                else
                {
                    return Forbid();
                }
                
                return result > 0 
                    ? Ok(new { data = result, message = "Specialist unmapped successfully.", statusCode = 200 })
                    : throw new Exception($"Unmapping failed");
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpGet("available")]
        [Authorize(Roles = "nurse,doctor,state_admin,coe_admin")]
        public async Task<IActionResult> GetAvailableSpecialists([FromQuery] int? coeId = null, [FromQuery] bool? onlineOnly = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var role = User.FindFirstValue(ClaimTypes.Role)!;
                
                // Get user info for debugging
                var currentUser = await _authService.GetProfile(userId);
                
                var specialists = await _service.GetAvailableSpecialists(userId, role, coeId);
                var specialistsList = specialists.ToList();
                
                // If onlineOnly is not specified, return only online and available specialists
                // If explicitly set to false, return all specialists
                // If explicitly set to true, return only online and available specialists
                var shouldFilterOnline = onlineOnly ?? true; // Default to true if not specified
                
                var availableSpecialists = shouldFilterOnline 
                    ? specialistsList.Where(s => s.IsOnline && !s.IsOnCall).ToList()
                    : specialistsList;
                
                return Ok(new { 
                    data = availableSpecialists, 
                    message = "Available specialists fetched successfully.", 
                    statusCode = 200,
                    totalCount = availableSpecialists.Count,
                    onlineCount = specialistsList.Count(s => s.IsOnline),
                    availableCount = specialistsList.Count(s => s.IsOnline && !s.IsOnCall)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = ex.Message, 
                    statusCode = 500,
                    stackTrace = ex.StackTrace 
                });
            }
        }
    }
}
