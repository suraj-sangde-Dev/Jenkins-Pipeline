using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeleICU.API.DTOs.BedsDtos;
using TeleICU.API.DTOs.SpokeDtos;
using TeleICU.API.Services;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Controllers
{
    [ApiController]
    [Route("api/spoke")]
    [Authorize]
    public class SpokeController : ControllerBase
    {
        private readonly ISpokeService _service;

        public SpokeController(ISpokeService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Roles = "super_admin,state_admin,coe_admin, spoke_admin, specialist, doctor, nurse")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                string role = User.FindFirstValue(ClaimTypes.Role)!;

                var spokes = await _service.GetSpokesByRole(userId, role);
                return Ok(new { data = spokes, message = "Spokes fetched successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var spoke = await _service.GetById(id);
                if (spoke == null)
                    return NotFound(new { message = "Spoke not found.", statusCode = 404 });

                return Ok(new { data = spoke, message = "Spoke fetched successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpPost]
        [Authorize(Roles = "state_admin,coe_admin")]
        public async Task<IActionResult> Create(CreateSpokeDto dto)
        {
            try
            {
                int createdBy = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _service.Create(dto, createdBy, Request);

                if (!result)
                    throw new Exception($"Failed to create spoke");

                return Ok(new { message = "Spoke created successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "state_admin,coe_admin")]
        public async Task<IActionResult> Update(int id, CreateSpokeDto dto)
        {
            try
            {
                var result = await _service.Update(id, dto, Request);
                if (!result)
                    return NotFound(new { message = "Spoke not found.", statusCode = 404 });

                return Ok(new { message = "Spoke updated successfully.", statusCode = 200 });
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
                if (!result)
                    return NotFound(new { message = "Spoke not found.", statusCode = 404 });

                return Ok(new { message = "Spoke deleted successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpPost("{id}/admin")]
        [Authorize(Roles = "state_admin,coe_admin")]
        public async Task<IActionResult> AssignAdmin(int id, [FromBody] AssignAdmins request)
        {
            try
            {
                var result = await _service.AssignAdmin(id, request.UserId);
                if (!result)
                    throw new Exception($"Failed to assign admin");

                return Ok(new { message = "Admin assigned successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpGet("{id}/admins")]
        [Authorize(Roles = "state_admin,coe_admin")]
        public async Task<IActionResult> GetAdmins(int id)
        {
            try
            {
                var admins = await _service.GetAdminsBySpokeId(id);
                return Ok(new { data = admins, message = "Admins fetched successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpGet("Unmapped-Spokes-by-stateId")]
        [Authorize(Roles = "state_admin")]
        public async Task<IActionResult> GetUnmappedSpokesForState()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var spokes = await _service.GetUnmappedSpokesByState(userId);

            return Ok(new { data = spokes, message = "Unmapped spokes fetched successfully.", statusCode = 200 });
        }

        [HttpPost("map-spoke")]
        [Authorize(Roles = "state_admin")]
        public async Task<IActionResult> MapSpokeWithCoe(MapSpokeRequest request)
        {
            var success = await _service.MapSpokeWithCoe(request.SpokeId, request.CoeId);
            if (!success)
                throw new Exception($"Failed to map spoke with CoE");

            return Ok(new { data = success, message = "Spoke mapped with CoE successfully.", statusCode = 200 });
        }

        [HttpDelete("unmap-spoke/{spokeId}")]
        [Authorize(Roles = "state_admin")]
        public async Task<IActionResult> UnmapSpokeFromCoe(int spokeId)
        {
            var success = await _service.UnmapSpokeFromCoe(spokeId);
            if (!success)
                return BadRequest(new { message = "Failed to unmap spoke from CoE." });

            return Ok(new { data = success, message = "Spoke unmapped from CoE successfully.", statusCode = 200 });
        }

        [HttpGet("mapped/{coeId}")]
        public async Task<IActionResult> GetMappedSpokes(int coeId)
        {
            var result = await _service.GetMappedSpokes(coeId);
            return Ok(new { data = result, message = "Fetched MappedSpokes successfully.", statusCode = 200 });
        }

        [HttpGet("for-specialist")]
        [Authorize(Roles = "specialist")]
        public async Task<IActionResult> GetSpokesForSpecialist()
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var spokes = await _service.GetSpokesForSpecialist(userId);
                return Ok(new { data = spokes, message = "Spokes fetched successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpPut("update-beds-only/{spokeId}")]
        public async Task<IActionResult> UpdateBedsOnly(int spokeId, [FromBody] BedCountUpdateDto dto)
        {
            var success = await _service.UpdateBedsOnly(spokeId, dto.IcuBeds, dto.HduBeds, dto.OtherBeds);
            return success ? Ok("Bed counts updated successfully.") : StatusCode(500, "Failed to update beds.");
        }


        public class MapSpokeRequest
        {
            public int SpokeId { get; set; }
            public int CoeId { get; set; }
        }
    }
}