using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeleICU.API.DTOs.CoeDtos;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Controllers
{
    [Route("api/coe")]
    [ApiController]
    [Authorize]
    public class CoeController : ControllerBase
    {
        private readonly ICoeService _service;
        private readonly IAuthService _authService;

        public CoeController(ICoeService service, IAuthService authService)
        {
            _service = service;
            _authService = authService;
        }

        [HttpGet]
        [Authorize(Roles = "super_admin,state_admin")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var role = User.FindFirstValue(ClaimTypes.Role)!;

                var result = await _service.GetAll(userId, role);
                return Ok(new { data = result, message = "CoEs fetched successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "super_admin,state_admin")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var coe = await _service.GetById(id);
                return coe == null
                    ? NotFound(new { message = "CoE not found.", statusCode = 404 })
                    : Ok(new { data = coe, message = "CoE fetched successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpPost]
        [Authorize(Roles = "super_admin,state_admin")]
        public async Task<IActionResult> Create(CreateCoeDto dto)
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var role = User.FindFirstValue(ClaimTypes.Role)!;

                int stateId;

                if (role == "state_admin")
                {
                    stateId = await GetStateIdForUser(userId);
                }
                else if (role == "super_admin")
                {
                    if (dto.StateId == null || dto.StateId == 0)
                        //return BadRequest(new { message = "Super Admin must provide a valid StateId.", statusCode = 400 });
                        throw new Exception($"Super Admin must provide a valid StateId");


                    stateId = dto.StateId.Value;
                }
                else return Forbid();

                var result = await _service.Create(dto, userId, stateId, Request);
                return result
                    ? Ok(new { message = "CoE created successfully.", statusCode = 200 })
                    : throw new Exception($"failed to create CoE");
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "super_admin,state_admin")]
        public async Task<IActionResult> Update(int id, [FromForm] CreateCoeDto dto)
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var role = User.FindFirstValue(ClaimTypes.Role)!;

                int stateId;

                if (role == "state_admin")
                {
                    stateId = await GetStateIdForUser(userId);
                }
                else if (role == "super_admin")
                {
                    return BadRequest(new { message = "Super Admin must include StateId in the DTO or modify flow to support it.", statusCode = 400 });
                }
                else return Forbid();

                var result = await _service.Update(id, dto, stateId, Request);
                return result
                    ? Ok(new { message = "CoE updated successfully.", statusCode = 200 })
                    : NotFound(new { message = "CoE not found.", statusCode = 404 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "state_admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _service.Delete(id);
                return result
                    ? Ok(new { message = "CoE deleted successfully.", statusCode = 200 })
                    : NotFound(new { message = "CoE not found.", statusCode = 404 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpPost("{id}/assign-admin")]
        [Authorize(Roles = "super_admin,state_admin")]
        public async Task<IActionResult> AssignAdmin(int id, AssignAdmin request)
        {
            try
            {
                var result = await _service.AssignAdmin(id, request.UserId);
                return result
                    ? Ok(new { message = "Admin assigned successfully.", statusCode = 200 })
                    : throw new Exception($"failed to assign admin");
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        private async Task<int> GetStateIdForUser(int userId)
        {
            var user = await _authService.GetProfile(userId);
            if (user?.StateId == null)
                throw new Exception("State ID not found for user.");
            return user.StateId.Value;
        }
    }
}
