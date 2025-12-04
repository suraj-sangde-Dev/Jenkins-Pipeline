using Dapper;
using TeleICU.API.DTOs.DoctorDtos;
using TeleICU.API.DTOs.NurseDtos;
using TeleICU.API.DTOs.SpecialistDtos;
using TeleICU.API.DTOs.StateDtos;
using TeleICU.API.Models.DoctorModels;
using TeleICU.API.Repository.Interface;
using static TeleICU.API.Services.DoctorService;
using static TeleICU.API.Services.NurseService;

namespace TeleICU.API.Repository
{
    public class DoctorRepository : IDoctorRepository
    {
        private readonly DapperContext _context;

        public DoctorRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DoctorDto>> GetBySpokeId(int spokeId)
        {
            var sql = @"
                SELECT d.DoctorId, d.UserId, d.SpokeId,
                       CONCAT(u.FirstName, ' ', u.MiddleName, ' ', u.LastName) AS FullName,
                       u.Email
                FROM Doctor d
                INNER JOIN Users u ON u.UserId = d.UserId
                WHERE d.SpokeId = @SpokeId";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<DoctorDto>(sql, new { SpokeId = spokeId });
        }

        public async Task<bool> AssignDoctor(DoctorModel doctor)
        {
            var sql = "INSERT IGNORE INTO Doctor (UserId, SpokeId) VALUES (@UserId, @SpokeId)";
            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(sql, doctor) > 0;
        }

        public async Task<bool> RemoveDoctor(int userId, int spokeId)
        {
            var sql = "DELETE FROM Doctor WHERE UserId = @UserId AND SpokeId = @SpokeId";
            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(sql, new { UserId = userId, SpokeId = spokeId }) > 0;
        }


        // Repository/DoctorRepository.cs
        public async Task<IEnumerable<DoctorDto>> GetAllDoctors()
        {
            var sql = @"
        SELECT 
            d.UserId,
            CONCAT_WS(' ', d.Title, d.FirstName, d.MiddleName, d.LastName) AS FullName,
            d.Qualification,
            d.Speciality,
            d.RegistrationNumber,
            d.Phone AS Contact,
            d.CreatedDate,
            d.IsActive,
            s.SpokeHospitalName
        FROM Users d
        INNER JOIN Doctor sm ON d.UserId = sm.UserId
        INNER JOIN Spokes s ON sm.SpokeId = s.SpokeId
        WHERE d.RoleId = 6";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<DoctorDto>(sql);
        }

        public async Task<IEnumerable<DoctorDto>> GetAllDoctorsByState(int stateId)
        {
            var sql = @"
        SELECT 
            d.UserId,
            CONCAT_WS(' ', d.Title, d.FirstName, d.MiddleName, d.LastName) AS FullName,
            d.Qualification,
            d.Speciality,
            d.RegistrationNumber,
            d.Phone AS Contact,
            d.CreatedDate,
            d.IsActive,
            s.SpokeHospitalName
        FROM Users d
        INNER JOIN Doctor sm ON d.UserId = sm.UserId
        INNER JOIN Spokes s ON sm.SpokeId = s.SpokeId
        WHERE d.RoleId = 6 AND d.StateId = @StateId";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<DoctorDto>(sql, new { StateId = stateId });
        }

        public async Task<IEnumerable<DoctorDto>> GetAllDoctorsBySpoke(int spokeId)
        {
            var sql = @"
        SELECT 
            d.UserId,
            CONCAT_WS(' ', d.Title, d.FirstName, d.MiddleName, d.LastName) AS FullName,
            d.Qualification,
            d.Speciality,
            d.RegistrationNumber,
            d.Phone AS Contact,
            d.CreatedDate,
            d.IsActive,
            s.SpokeHospitalName
        FROM Users d
        INNER JOIN Doctor sm ON d.UserId = sm.UserId
        INNER JOIN Spokes s ON sm.SpokeId = s.SpokeId
        WHERE d.RoleId = 6 AND sm.SpokeId = @SpokeId";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<DoctorDto>(sql, new { SpokeId = spokeId });
        }


        public async Task<DoctorResponseDto> GetDoctorById(int id)
        {
            var sql = @"
                    SELECT 
                        u.UserId,
                        u.Username,
                        u.Email,
                        u.Phone,
                        CONCAT_WS(' ', u.Title, u.FirstName, u.MiddleName, u.LastName) AS FullName,
                        u.RoleId,
                        sm.SpokeId,
                        u.ProfilePic,
                        u.Gender,
                        u.DOB,
                        u.Language,
                        u.SignaturePath,
                        u.AddressLine1,
                        u.AddressLine2,
                        s.StateId AS StateId, s.StateCode AS StateCode, s.StateName AS StateName,
                        d.DistrictId AS DistrictId, d.DistrictCode AS DistrictCode, d.DistrictName AS DistrictName,
                        ci.CityId AS CityId, ci.CityCode AS CityCode, ci.CityName AS CityName,
                        u.PIN,
                        u.FacebookProfile,
                        u.TwitterProfile,
                        u.LinkedInProfile,
                        u.RegistrationNumber,
                        u.Qualification,
                        u.Speciality,
                        u.Experience,
                        u.CreatedBy,
                        u.IsActive,
                        u.CreatedDate
                    FROM Users u
                    INNER JOIN Doctor sm ON u.UserId = sm.UserId
                    INNER JOIN Spokes c ON sm.SpokeId = c.SpokeId
                    LEFT JOIN States s ON u.State = s.StateCode
                    LEFT JOIN Districts d ON u.District = d.DistrictCode
                    LEFT JOIN Cities ci ON u.City = ci.CityCode
                    WHERE u.UserId = @UserId AND u.RoleId = 6";

            using var connection = _context.CreateConnection();
            var result = await connection.QueryAsync<DoctorResponseDto, StateDto, DistrictDto, CityDto, DoctorResponseDto>(
                sql,
                (doctor, state, district, city) =>
                {
                    doctor.States = state;
                    doctor.District = district;
                    doctor.City = city;
                    return doctor;
                },
                new { UserId = id },
                splitOn: "StateId,DistrictId,CityId"
            );
            return result.FirstOrDefault();
        }


        public async Task<IEnumerable<DoctorSimpleDto>> GetUnmappedDoctorsByState(int stateId)
        {
            var sql = @"
                      SELECT 
                          u.UserId,
                          CONCAT(u.FirstName, ' ', IFNULL(u.LastName, '')) AS FullName
                      FROM Users u
                      WHERE u.RoleId = 6
                        AND u.StateId = @StateId
                        AND (u.SpokeId IS NULL OR u.SpokeId = 0)";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<DoctorSimpleDto>(sql, new { StateId = stateId });
        }

        public async Task<IEnumerable<DoctorSimpleDto>> GetMappedDoctorsBySpoke(int spokeId)
        {
            var sql = @"
                      SELECT 
                          u.UserId,
                          CONCAT(u.FirstName, ' ', IFNULL(u.LastName, '')) AS FullName
                      FROM Users u
                      WHERE u.RoleId = 6
                        AND u.SpokeId = @SpokeId";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<DoctorSimpleDto>(sql, new { SpokeId = spokeId });
        }
         
        public async Task<int> MapDoctorToSpoke(int doctorId, int spokeId)
        {
            var sql = @"UPDATE Users SET SpokeId = @SpokeId WHERE UserId = @DoctorId AND RoleId = 6";
            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(sql, new { DoctorId = doctorId, SpokeId = spokeId });
        }
         
        public async Task<int> UnmapDoctor(int doctorId)
        {
            var sql = @"UPDATE Users SET SpokeId = NULL WHERE UserId = @DoctorId AND RoleId = 6";
            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(sql, new { DoctorId = doctorId });
        }
    }
} 