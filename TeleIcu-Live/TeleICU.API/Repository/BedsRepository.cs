using Dapper;
using TeleICU.API.DTOs.DoctorDtos;
using TeleICU.API.DTOs.SpokeDtos;
using TeleICU.API.Repository.Interface;

namespace TeleICU.API.Repository
{
    public class BedsRepository : IBedsRepository
    {
        private readonly DapperContext _context;

        public BedsRepository(DapperContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<BedsDto>> GetBeds(int spokeId)
        {
            var sql = @"
       SELECT 
            b.BedId,
            b.BedType,
            b.BedNumber,
            b.Status,
            p.PatientId,
            CONCAT(p.FirstName, ' ', IFNULL(p.LastName, '')) AS FullName,
            p.AdmitDate,
            TIMESTAMPDIFF(YEAR, DOB, CURDATE()) AS Age
       FROM Beds b
       LEFT JOIN Patients p 
            ON b.BedId = p.BedId
       WHERE b.SpokeId = @SpokeId"; 
        
    using var conn = _context.CreateConnection();
            return await conn.QueryAsync<BedsDto>(sql, new { SpokeId = spokeId });
        }

    }
}

