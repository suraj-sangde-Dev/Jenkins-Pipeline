using TeleICU.API.DTOs.MedicineDto;
using TeleICU.API.DTOs.NurseDtos;

namespace TeleICU.API.Services.Interface
{
    public interface IMedicineService
    {
        Task<IEnumerable<MedicineDto>> GetAllMedicines();
        Task<bool> AddMedicinesAsync(List<MedicineDto> medicines);
    }
}
