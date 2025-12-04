using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeleICU.API.DTOs.PatientDto;
using TeleICU.API.Models.AuthModels;
using TeleICU.API.Services;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Controllers
{
    [Route("api/patient")]
    [ApiController]
    public class PatientController : ControllerBase
    {
        private readonly IPatientService _service;

        public PatientController(IPatientService service)
        {
            _service = service;
        }

        [HttpGet("vacant-beds")]
        [Authorize(Roles = "doctor,nurse")] // only doctors/nurses
        public async Task<IActionResult> GetVacantBeds()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var beds = await _service.GetVacantBeds(userId);

            return Ok(new { data = beds, message = "Vacant beds fetched successfully", statusCode = 200 });
        }


        [HttpPost("register")]
        [Authorize(Roles = "nurse")]
        public async Task<IActionResult> RegisterPatient( RegisterPatientRequest request)
        {
            var createdBy = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _service.RegisterPatient(request, createdBy);

            return Ok(new { data = response, message = "Patient registered successfully.", statusCode = 200 });
        }

        [HttpGet("doctors")]
        [Authorize(Roles = "doctor,nurse")]
        public async Task<IActionResult> GetDoctors()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var doctors = await _service.GetDoctorsForPatient(userId);

            if (!doctors.Any())
                return BadRequest(new { message = "No doctors found or user not assigned to any spoke hospital" });

            return Ok(new { data = doctors, message = "Doctors fetched successfully", statusCode = 200 });
        }

        [HttpPut("discharge/{patientId}")]
        [Authorize]
        public async Task<IActionResult> DischargePatient(int patientId)
        {
            var result = await _service.DischargePatient(patientId);

            if (result)
                return Ok(new { message = "Patient discharged and bed freed successfully", statusCode = 200 });
            else
                return BadRequest(new { message = "Failed to discharge patient", statusCode = 400 });
        }

        [HttpGet("list")]
        [Authorize(Roles = "doctor,nurse, specialist")]
        public async Task<IActionResult> GetPatients()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var patients = await _service.GetPatientsForUser(userId);

            if (!patients.Any())
                return Ok(new { data = new List<object>(), message = "No patients found", statusCode = 200 });

            return Ok(new { data = patients, message = "Patients fetched successfully", statusCode = 200 });
        }

        [HttpGet("tiles")]
        [Authorize(Roles = "doctor,nurse,specialist")]
        public async Task<IActionResult> GetPatientTiles()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var tiles = await _service.GetPatientTilesForUser(userId);
            if (!tiles.Any())
                return NotFound(new { message = "No Patient Tiles found " });
            return Ok(new { data = tiles, message = "Patient tiles fetched successfully", statusCode = 200 });
        }

        [HttpGet("vital-trends/{patientId}")]
        [Authorize(Roles = "doctor,nurse, specialist")]
        public async Task<IActionResult> GetVitalTrends(int patientId, [FromQuery] string timeRange)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var request = new VitalTrendRequest
                {
                    PatientId = patientId,
                    TimeRange = timeRange
                };
                var trends = await _service.GetVitalTrends(userId, request);
                return Ok(new { data = trends, message = "Vital trends fetched successfully", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpGet("medications")]
        [Authorize(Roles = "doctor,nurse, specialist")]
        public async Task<IActionResult> GetMedications([FromQuery] PatientMedicationQuery query)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var meds = await _service.GetPatientMedications(userId, query);
            return Ok(new { data = meds, message = "Medications fetched successfully", statusCode = 200 });
        }

        [HttpPost("{patientId}/vitals")]
        [Authorize(Roles = "doctor,nurse, specialist")]
        public async Task<IActionResult> RecordVital(int patientId, [FromBody] TeleICU.API.DTOs.CaseDto.VitalDto request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _service.RecordVital(userId, patientId, request);
            return Ok(new { message = "Vital recorded successfully", statusCode = 200 });
        }

        [HttpGet("{patientId}")]
        [Authorize(Roles = "doctor,nurse, specialist")]
        public async Task<IActionResult> GetPatientById(int patientId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            var patient = await _service.GetPatientById(userId, patientId);
            
            if (patient == null)
                return NotFound(new { message = "Patient not found or access denied", statusCode = 404 });

            return Ok(new { data = patient, message = "Patient details fetched successfully", statusCode = 200 });
        }

        [HttpPut("edit-patient")]
        [Authorize(Roles = "doctor,nurse, specialist")]
        public async Task<IActionResult> UpdatePatient([FromBody] EditPatientRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                
                var result = await _service.UpdatePatient(userId, request);
                
                if (!result)
                    return BadRequest(new { message = "Failed to update patient", statusCode = 400 });

                return Ok(new { message = "Patient updated successfully", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }
    }
}
