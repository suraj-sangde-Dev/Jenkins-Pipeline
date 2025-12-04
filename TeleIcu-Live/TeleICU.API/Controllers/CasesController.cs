using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeleICU.API.DTOs.CaseDto;
using TeleICU.API.Services.Interface;
using System.Linq;

namespace TeleICU.API.Controllers
{
    [Route("api/cases")]
    [ApiController]
    public class CasesController : ControllerBase
    {
        private readonly ICaseService _service;

        public CasesController(ICaseService service)
        {
            _service = service;
        }

        [HttpPost("create-case")]
        [Authorize(Roles = "doctor,nurse, specialist")]
        public async Task<IActionResult> CreateCase([FromBody] CreateCaseDto request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var caseId = await _service.CreateCaseAsync(request, userId);

            return Ok(new { data = new { CaseId = caseId }, message = "Case created successfully", statusCode = 200 });
        }

        [HttpPost("add-history")]
        [Authorize(Roles = "doctor,nurse")]
        public async Task<IActionResult> AddHistory([FromBody] CaseHistoryDto request)
        {
            await _service.AddHistoryAsync(request);
            return Ok(new { message = "History added successfully", statusCode = 200 });
        }

       
        [HttpPost("add-medication")]
        [Authorize(Roles = "doctor,nurse")]
        public async Task<IActionResult> AddMedications([FromBody] IEnumerable<PreAdmitMedicationDto> requests)
        {
            if (requests == null)
                return BadRequest(new { message = "No medications provided", statusCode = 400 });

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Default PrescribedBy to current user when not set
            foreach (var r in requests)
            {
                if (r != null && r.PrescribedBy == 0)
                    r.PrescribedBy = userId;
            }

            await _service.AddMedicationsAsync(requests);

            // Return the latest list for the first case id in the payload
            var caseId = requests.First().CaseId;
            var list = await _service.GetPreAdmitMedicationsAsync(caseId);
            return Ok(new { data = list, message = "Medications added successfully", statusCode = 200 });
        }

        [HttpGet("{caseId}/medications")]
        [Authorize(Roles = "doctor,nurse, specialist")]
        public async Task<IActionResult> GetPreAdmitMedications(int caseId)
        {
            var list = await _service.GetPreAdmitMedicationsAsync(caseId);
            return Ok(new { data = list, message = "Medications fetched successfully", statusCode = 200 });
        }

        [HttpPost("add-vital")]
        [Authorize(Roles = "doctor,nurse, specialist")]
        public async Task<IActionResult> AddVital([FromBody] VitalDto request)
        {
            await _service.AddVitalAsync(request);
            return Ok(new { message = "Vital added successfully", statusCode = 200 });
        }

        [HttpPost("upload")]
        [Authorize(Roles = "doctor,nurse, specialist")]
        public async Task<IActionResult> UploadHealthRecords([FromForm] CaseHealthRecordUploadDto request)
        {
            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "HealthRecords");
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

             if (request.File == null || request.File.Count == 0)
            {
                return BadRequest(new { message = "No files provided.", statusCode = 400 });
            }

            if (request.RecordTypes == null || request.RecordTypes.Count != request.File.Count)
            {
                return BadRequest(new { message = "RecordTypes count must match files count.", statusCode = 400 });
            }

            var uploadedFiles = new List<string>();

            foreach (var file in request.File)
            {
                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var filePathUrl = $"{baseUrl}/uploads/HealthRecords/{fileName}";

                uploadedFiles.Add(filePathUrl);
            }

            await _service.UploadHealthRecordsAsync(request, uploadedFiles);

            return Ok(new
            {
                message = "Health records uploaded successfully",
                files = uploadedFiles,
                statusCode = 200
            });
        }


        [HttpPost("add-query")]
        [Authorize(Roles = "doctor,nurse, specialist")]
        public async Task<IActionResult> AddCaseQuery([FromBody] CaseQueryDto request)
        {
            await _service.AddCaseQueryAsync(request);

            return Ok(new
            {
                message = "Case query saved successfully",
                statusCode = 200
            });
        }


        [HttpGet("{caseId}/view")]
        [Authorize(Roles = "doctor,nurse,specialist")]
        public async Task<IActionResult> GetCaseView(int caseId)
        {
            var view = await _service.GetCaseViewAsync(caseId);
            if (view == null)
                return NotFound(new { message = "Case not found", statusCode = 404 });

            return Ok(new { data = view, message = "Case view fetched successfully", statusCode = 200 });
        }

        [HttpPost("final-submit")]
        [Authorize(Roles = "doctor,nurse, ")]
        public async Task<IActionResult> FinalSubmit(int caseId)
        {   
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var success = await _service.ActivateCaseAsync(caseId, userId);

            if (!success)
                return NotFound(new { message = "Case not found or already active", statusCode = 404 });

            return Ok(new { message = "Case submitted successfully", statusCode = 200 });
        }
    }
}
