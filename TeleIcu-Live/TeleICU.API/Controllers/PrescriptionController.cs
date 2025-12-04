using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Dapper;
using TeleICU.API.DTOs.PrescriptionDtos;
using TeleICU.API.Helpers;
using TeleICU.API.Hubs;
using TeleICU.API.Services;
using TeleICU.API.Services.Interface;
using System.Runtime.CompilerServices;

namespace TeleICU.API.Controllers
{
    [ApiController]
    [Route("api/prescription")]
    [Authorize]
    public class PrescriptionController : ControllerBase
    {
        private readonly IPrescriptionService _service;
        private readonly IHubContext<CallHub> _hubContext;
        private readonly ILogger<PrescriptionController> _logger;
        private readonly EncounterStorageService _encounterStorage;

        public PrescriptionController(IPrescriptionService service, IHubContext<CallHub> hubContext, 
            ILogger<PrescriptionController> logger, EncounterStorageService encounterStorage)
        {
            _service = service;
            _hubContext = hubContext;
            _logger = logger;
            _encounterStorage = encounterStorage;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncPrescription([FromBody] SyncPrescriptionDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var syncedData = await _service.SyncPrescriptionAsync(dto, userId, dto.EncounterId);

                if (syncedData == null)
                    return BadRequest(new { message = "Failed to sync prescription.", statusCode = 400 });

                // Broadcast to both participants (nurse & specialist) if EncounterId is provided
                if (!string.IsNullOrEmpty(dto.EncounterId))
                {
                    try
                    {
                        var encounterData = await _encounterStorage.GetEncounterAsync(dto.EncounterId);
                        if (encounterData != null)
                        {
                            // Get connection IDs for both participants
                            var callerConnectionId = await _encounterStorage.GetUserConnectionAsync(encounterData.CallerId);
                            var calleeConnectionId = await _encounterStorage.GetUserConnectionAsync(encounterData.CalleeId);

                            var prescriptionData = new
                            {
                                caseId = syncedData.CaseId,
                                encounterId = syncedData.EncounterId,
                                examination = syncedData.Examination,
                                advice = syncedData.Advice,
                                medications = syncedData.Medications,
                                timestamp = DateTime.UtcNow
                            };

                            // Send to caller (specialist) if online
                            if (!string.IsNullOrEmpty(callerConnectionId))
                            {
                                await _hubContext.Clients.Client(callerConnectionId)
                                    .SendAsync("PrescriptionSynced", prescriptionData);
                            }

                            // Send to callee (nurse) if online
                            if (!string.IsNullOrEmpty(calleeConnectionId))
                            {
                                await _hubContext.Clients.Client(calleeConnectionId)
                                    .SendAsync("PrescriptionSynced", prescriptionData);
                            }

                            _logger.LogInformation("Prescription sync broadcasted to participants in encounter {EncounterId}", dto.EncounterId);
                        }
                        else
                        {
                            _logger.LogWarning("Encounter {EncounterId} not found for prescription sync broadcast", dto.EncounterId);
                        }
                    }
                    catch (Exception broadcastEx)
                    {
                        _logger.LogError(broadcastEx, "Failed to broadcast prescription sync for encounter {EncounterId}", dto.EncounterId);
                    }
                }

                return Ok(new { message = "Prescription synced successfully.", data = syncedData, statusCode = 200 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing prescription for case {CaseId}", dto.CaseId);
                return StatusCode(500, new { message = ex.Message, statusCode = 500 });
            }
        }

        /// <summary>
        /// Generate and save prescription PDF for a consultation
        /// Called when specialist clicks "Send Prescription"
        /// Uses ConsultationId instead of CallId
        /// </summary>
        [HttpGet("generate-pdf")]
        [Authorize(Roles = "specialist,nurse")]
        public async Task<IActionResult> GeneratePrescriptionPdf([FromQuery] string ConsultationId)
        {
            try
            {
                // ConsultationId = Uri.UnescapeDataString(ConsultationId);
                if (string.IsNullOrEmpty(ConsultationId))
                {
                    return BadRequest(new { message = "ConsultationId is required", statusCode = 400 });
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var filePathUrl = await _service.GenerateAndSavePrescriptionPdfByConsultationIdAsync(ConsultationId, baseUrl);

                return Ok(new
                {
                    success = true,
                    message = "Prescription PDF generated and saved successfully",
                    data = new { filePath = filePathUrl },
                    statusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating prescription PDF for ConsultationId: {ConsultationId}",
                    ConsultationId);
                return StatusCode(500, new { message = $"Error generating PDF: {ex.Message}", statusCode = 500 });
            }
        }

        /// <summary>
        /// Download prescription PDF for a consultation
        /// Uses ConsultationId instead of CallId
        /// </summary>
        [HttpGet("download/{*consultationId}")]
        [Authorize(Roles = "nurse,doctor,specialist,coe_admin")]
        public async Task<IActionResult> DownloadPrescriptionPdf(string consultationId)
        {
            try
            {
                // Normalize consultationId to handle URL-encoded slashes and other characters
                if (!string.IsNullOrWhiteSpace(consultationId))
                {
                    consultationId = Uri.UnescapeDataString(consultationId);
                }
                // Get case ID from ConsultationId
                var dapperContext = HttpContext.RequestServices.GetRequiredService<DapperContext>();
                using var connection = dapperContext.CreateConnection();
                
                // First try to find from CasePreAdmitMedications
                var caseId = await connection.ExecuteScalarAsync<int?>(
                    @"SELECT DISTINCT CaseId FROM CasePreAdmitMedications 
                      WHERE ConsultationId = @ConsultationId 
                      LIMIT 1",
                    new { ConsultationId = consultationId });

                // If not found, try CallLogs (fallback for old records)
                if (!caseId.HasValue)
                {
                    caseId = await connection.ExecuteScalarAsync<int?>(
                        "SELECT CaseId FROM CallLogs WHERE CallId = @ConsultationId LIMIT 1",
                        new { ConsultationId = consultationId });
                }

                if (!caseId.HasValue)
                {
                    return NotFound(new { message = "Consultation or case not found", statusCode = 404 });
                }

                // Get prescription PDF from CaseHealthRecords
                // Match by ConsultationId stored in FilePath or by CaseId
                var prescription = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT FilePath FROM CaseHealthRecords 
                      WHERE CaseId = @CaseId AND RecordType = 'Prescription' 
                      AND (FilePath LIKE @ConsultationIdPattern OR FilePath LIKE @ConsultationIdPattern2)
                      ORDER BY UploadedDate DESC LIMIT 1",
                    new { 
                        CaseId = caseId.Value,
                        ConsultationIdPattern = $"%{consultationId}%",
                        ConsultationIdPattern2 = $"%{consultationId.Replace("/", "%")}%"
                    });

                // If not found with pattern, get latest prescription for the case
                if (prescription == null || string.IsNullOrEmpty(prescription.FilePath?.ToString()))
                {
                    prescription = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        @"SELECT FilePath FROM CaseHealthRecords 
                          WHERE CaseId = @CaseId AND RecordType = 'Prescription' 
                          ORDER BY UploadedDate DESC LIMIT 1",
                        new { CaseId = caseId.Value });
                }

                if (prescription == null || string.IsNullOrEmpty(prescription.FilePath?.ToString()))
                {
                    return NotFound(new { message = "Prescription PDF not found", statusCode = 404 });
                }

                var filePath = prescription.FilePath.ToString();
                
                // Extract local file path from URL
                var localPath = filePath.Replace($"{Request.Scheme}://{Request.Host}/", "");
                localPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", localPath);

                if (!System.IO.File.Exists(localPath))
                {
                    return NotFound(new { message = "PDF file not found on server", statusCode = 404 });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(localPath);
                var fileName = Path.GetFileName(localPath);

                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading prescription PDF for ConsultationId: {ConsultationId}", consultationId);
                return StatusCode(500, new { message = $"Error downloading PDF: {ex.Message}", statusCode = 500 });
            }
        }
    }

    public class GeneratePrescriptionPdfRequest
    {
        public string ConsultationId { get; set; } = string.Empty;
    }
}
