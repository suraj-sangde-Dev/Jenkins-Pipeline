using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Controllers
{
    [ApiController]
    [Route("api/state")]
    [Authorize]
    public class StateController : ControllerBase
    {
        private readonly IStateService _service;

        public StateController(IStateService service)
        {
            _service = service;
        }

        [HttpGet("get-all-states")]
        public async Task<IActionResult> GetAll()
        {
            try
            {


                var states = await _service.GetAllStates();
                return Ok(new { data = states, message = "States fetched successfully.", statusCode = 200 });
            }
            catch (Exception)
            {
                throw new Exception($"Error fetching states");
            }
        }

        [HttpGet("districts/{stateCode}")]
        public async Task<IActionResult> GetDistricts(int stateCode)
        {
            try
            {
                var result = await _service.GetDistrictsAsync(stateCode);
                return Ok(new { data = result, message = "Districts fetched successfully.", statusCode = 200 });
            }
            catch (Exception)
            {
                //return StatusCode(500, new { message = $"Error fetching districts: {ex.Message}", statusCode = 500 }); 
                throw new Exception($"Error fetching districts");
            }
        }

        [HttpGet("cities/{districtCode}")]
        public async Task<IActionResult> GetCities(int districtCode)
        {
            try
            {
                var result = await _service.GetCitiesAsync(districtCode);
                return Ok(new { data = result, message = "Cities fetched successfully.", statusCode = 200 });
            }
            catch (Exception)
            {
                throw new Exception($"Error fetching districts");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var state = await _service.GetById(id);
                if (state == null)
                    return NotFound(new { message = "State not found.", statusCode = 404 });

                return Ok(new { data = state, message = "State fetched successfully.", statusCode = 200 });
            }
            catch (Exception)
            {
                throw new Exception($"Error retrieving states");
            }
        }

        [HttpDelete("unassign-admin/{stateId}")]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> UnassignStateAdmin(int stateId)
        {
            try
            {
                var result = await _service.UnassignStateAdmin(stateId);
                if (!result)
                    return NotFound(new { message = "Mapping not found.", statusCode = 404 });

                return Ok(new { message = "State admin unmapped successfully.", statusCode = 200 });
            }
            catch (Exception)
            {
                throw new Exception($"Error unassigning state admin");
            }
        }

        [HttpGet("{stateId}/admin")]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> GetAdminByState(int stateId)
        {
            try
            {
                var admin = await _service.GetAdminByStateId(stateId);
                if (admin == null)
                    return NotFound(new { message = "No admin assigned to this state.", statusCode = 404 });

                return Ok(new { data = admin, message = "Admin fetched successfully.", statusCode = 200 });
            }
            catch (Exception)
            {
                throw new Exception($"Error fetching state admin");
            }
        }

        [HttpGet("assigned")]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> GetAssignedStates()
        {
            try
            {
                var states = await _service.GetAssignedStates();
                return Ok(new { data = states, message = "Assigned states fetched successfully.", statusCode = 200 });
            }
            catch (Exception)
            {
                throw new Exception($"Error fetching assigned states");
            }
        }

        [HttpGet("unassigned")]
        [Authorize(Roles = "super_admin")]
        public async Task<IActionResult> GetUnassignedStates()
        {
            try
            {
                var states = await _service.GetUnassignedStates();
                return Ok(new { data = states, message = "Unassigned states fetched successfully.", statusCode = 200 });
            }
            catch (Exception)
            {
                throw new Exception($"Error fetching unassigned states");
            }
        }
    }
}
