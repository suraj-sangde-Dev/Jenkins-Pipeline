using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TeleICU.API.DTOs.MedicineDto;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Controllers
{
    [Route("api/medicine")]
    [ApiController]
    public class MedicineController : ControllerBase
    {
        private readonly IMedicineService _medicineService;
        public MedicineController(IMedicineService medicineService)
        {
            _medicineService = medicineService;
        }
        [HttpGet("getallmedicine")]
        [Authorize(Roles = "doctor,nurse,specialist")]
        public async Task<IActionResult> GetMedicineDropdown()
        {
            try
            {
                var medicine = await _medicineService.GetAllMedicines();
                return Ok(new { data = medicine, message = "Medicine details fetched successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpPost("add-medicines")]

        public async Task<IActionResult> AddMedicines([FromBody] List<MedicineDto> medicines)
        {
            try
            {
                if (medicines == null || !medicines.Any())
                {
                    return BadRequest(new { message = "Medicine list cannot be empty.", statusCode = 400 });
                }

                var result = await _medicineService.AddMedicinesAsync(medicines);

                if (result)
                {
                    return Ok(new { message = "Medicines inserted successfully.", statusCode = 200 });
                }

                return BadRequest(new { message = "Failed to insert medicines.", statusCode = 400 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

    }
}
