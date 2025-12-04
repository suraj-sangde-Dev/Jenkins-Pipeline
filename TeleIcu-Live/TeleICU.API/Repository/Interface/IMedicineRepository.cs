using TeleICU.API.DTOs.MedicineDto;
using TeleICU.API.DTOs.NurseDtos;

namespace TeleICU.API.Repository.Interface
{
    public interface IMedicineRepository
    {
        Task<IEnumerable<MedicineDto>> GetAllMedicines();
        Task<bool> AddMedicinesAsync(List<MedicineDto> medicines);
    }
}
