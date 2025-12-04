using Dapper;
using TeleICU.API.DTOs.DoctorDtos;
using TeleICU.API.DTOs.MedicineDto;
using TeleICU.API.DTOs.NurseDtos;
using TeleICU.API.Repository.Interface;

namespace TeleICU.API.Repository
{
    public class MedicineRepository : IMedicineRepository
    {
        private readonly DapperContext _context;

        public MedicineRepository(DapperContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<MedicineDto>> GetAllMedicines()
        {
            var sql = @" SELECT 
            id, name from Medicines
            ";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<MedicineDto>(sql);
        }

        public async Task<bool> AddMedicinesAsync(List<MedicineDto> medicines)
        {
            var sql = @"INSERT INTO Medicines (Name) VALUES (@Name);";

            using var conn = _context.CreateConnection();

            // 🔴 IMPORTANT : OPEN CONNECTION
            if (conn.State == System.Data.ConnectionState.Closed)
                conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                foreach (var med in medicines)
                {
                    await conn.ExecuteAsync(sql, new { med.Name }, transaction);
                }

                transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                transaction.Rollback();
                return false;
            }
                }

    }
}
