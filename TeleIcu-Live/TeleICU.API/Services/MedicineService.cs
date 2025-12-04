using TeleICU.API.DTOs.MedicineDto;
using TeleICU.API.DTOs.NurseDtos;
using TeleICU.API.Repository.Interface;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Services
{
    public class MedicineService : IMedicineService
    {
        private readonly IMedicineRepository _repo;

        public MedicineService(IMedicineRepository repo)
        {
            _repo = repo;
        }
        public async Task<IEnumerable<MedicineDto>> GetAllMedicines()
        {
            var medicine = await _repo.GetAllMedicines();
            return medicine;
        }
        public async Task<bool> AddMedicinesAsync(List<MedicineDto> medicines)
        {
            if (medicines == null || medicines.Count == 0)
                return false;

            // Call repository to insert
            var result = await _repo.AddMedicinesAsync(medicines);

            return result;
        }
    }
}
