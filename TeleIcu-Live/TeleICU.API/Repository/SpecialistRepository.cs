using Dapper;
using TeleICU.API.DTOs.SpecialistDtos;
using TeleICU.API.DTOs.StateDtos;
using TeleICU.API.Models.SpecialistModels;
using TeleICU.API.Repository.Interface;
using static TeleICU.API.Services.SpecialistService;

namespace TeleICU.API.Repository
{
    public class SpecialistRepository : ISpecialistRepository
    {
        private readonly DapperContext _context;

        public SpecialistRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SpecialistDto>> GetByCoeId(int coeId)
        {
            var sql = @"
                SELECT s.SpecialistId, s.UserId, s.CoeId,
                       CONCAT(u.FirstName, ' ', u.MiddleName, ' ', u.LastName) AS FullName,
                       u.Email
                FROM SpecialistsMapping s
                INNER JOIN Users u ON u.UserId = s.UserId
                WHERE s.CoeId = @CoeId";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<SpecialistDto>(sql, new { CoeId = coeId });
        }

        public async Task<bool> AssignSpecialist(SpecialistModel specialist)
        {
            var sql = @"INSERT IGNORE INTO SpecialistsMapping (UserId, CoeId) VALUES (@UserId, @CoeId)";
            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(sql, specialist) > 0;
        }

        public async Task<bool> RemoveSpecialist(int userId, int coeId)
        {
            var sql = "DELETE FROM SpecialistsMapping WHERE UserId = @UserId AND CoeId = @CoeId";
            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(sql, new { UserId = userId, CoeId = coeId }) > 0;
        }

        public async Task<IEnumerable<AllSpecialistDto>> GetAllSpecialists(int userId, string role)
        {
            string sql;
            object parameters;

            if (role == "super_admin")
            {
                // Super admin can see all specialists
                sql = @"
        SELECT 
            u.UserId,
            CONCAT_WS(' ', u.Title, u.FirstName, u.MiddleName, u.LastName) AS FullName,
            u.Qualification,
            u.Speciality,
            c.CoeName,
            u.RegistrationNumber,
            u.Phone,
            u.CreatedDate,
            u.IsActive
        FROM Users u
        INNER JOIN SpecialistsMapping sm ON u.UserId = sm.UserId
        INNER JOIN CentersOfExcellence c ON sm.CoeId = c.CoeId
        WHERE u.RoleId = 5";
                parameters = new { };
            }
            else
            {
                // State admin and COE admin can only see specialists from their state
                sql = @"
        SELECT 
            u.UserId,
            CONCAT_WS(' ', u.Title, u.FirstName, u.MiddleName, u.LastName) AS FullName,
            u.Qualification,
            u.Speciality,
            c.CoeName,
            u.RegistrationNumber,
            u.Phone,
            u.CreatedDate,
            u.IsActive
        FROM Users u
        INNER JOIN SpecialistsMapping sm ON u.UserId = sm.UserId
        INNER JOIN CentersOfExcellence c ON sm.CoeId = c.CoeId
        WHERE u.RoleId = 5 AND u.StateId = (SELECT StateId FROM Users WHERE UserId = @UserId)";
                parameters = new { UserId = userId };
            }

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<AllSpecialistDto>(sql, parameters);
        }

        public async Task<SpecialistResponseDto> GetSpecialistById(int id)
        {
            var sql = @"
SELECT 
    u.UserId,
    u.Username,
    u.Email,
    u.Title,
    u.FirstName,
    u.MiddleName,
    u.LastName,
    u.Phone,
    CONCAT_WS(' ', u.Title, u.FirstName, u.MiddleName, u.LastName) AS FullName,
    u.RoleId,
    sm.CoeId,
    c.CoeName,
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
    INNER JOIN SpecialistsMapping sm ON u.UserId = sm.UserId
    INNER JOIN CentersOfExcellence c ON sm.CoeId = c.CoeId
    LEFT JOIN States s ON u.State = s.StateCode
    LEFT JOIN Districts d ON u.District = d.DistrictCode
    LEFT JOIN Cities ci ON u.City = ci.CityCode
    WHERE u.UserId = @UserId AND u.RoleId = 5";

            using var connection = _context.CreateConnection();
            var result = await connection.QueryAsync<SpecialistResponseDto, StateDto, DistrictDto, CityDto, SpecialistResponseDto>(
                sql,
                (specialist, state, district, city) =>
                {
                    specialist.State = state;
                    specialist.District = district;
                    specialist.City = city;
                    return specialist;
                },
                new { UserId = id },
                splitOn: "StateId,DistrictId,CityId"
            );
            return result.FirstOrDefault();
        }


        public async Task<IEnumerable<SpecialistSimpleDto>> GetUnmappedSpecialistsByState(int stateId)
        {
            var sql = @"
        SELECT 
            u.UserId ,
u.Title,
            CONCAT(u.FirstName, ' ', IFNULL(u.LastName, '')) AS FullName
        FROM Users u
        WHERE u.RoleId = 5 
          AND u.StateId = @StateId
          AND (u.CoeId IS NULL OR u.CoeId = 0)";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<SpecialistSimpleDto>(sql, new { StateId = stateId });
        }

        public async Task<IEnumerable<SpecialistSimpleDto>> GetMappedSpecialistsByCoe(int coeId)
        {
            var sql = @"
                SELECT 
                    u.UserId,
u.Title,
                    CONCAT(u.Title, ' ', u.FirstName, ' ', IFNULL(u.LastName, '')) AS FullName
                FROM Users u
                WHERE u.RoleId = 5
                  AND u.CoeId = @CoeId";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<SpecialistSimpleDto>(sql, new { CoeId = coeId });
        }

        public async Task<int> MapSpecialistToCoe(int specialistId, int coeId)
        {
            var sql = @"UPDATE Users SET CoeId = @CoeId WHERE UserId = @SpecialistId AND RoleId = 5";
            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(sql, new { SpecialistId = specialistId, CoeId = coeId });
        }

        public async Task<int> UnmapSpecialist(int specialistId)
        {
            var sql = @"UPDATE Users SET CoeId = NULL WHERE UserId = @SpecialistId AND RoleId = 5";
            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(sql, new { SpecialistId = specialistId });
        }

        public async Task<IEnumerable<AvailableSpecialistDto>> GetAllSpecialistsByState(int stateId)
        {
            var sql = @"
                SELECT 
                    u.UserId,
                    CONCAT(u.Title, ' ', u.FirstName, ' ', IFNULL(u.LastName, '')) AS FullName,
                    u.Speciality,
                    IFNULL(c.CoeName, 'Not Assigned') AS CoeName
                FROM Users u
                LEFT JOIN CentersOfExcellence c ON u.CoeId = c.CoeId
                WHERE u.RoleId = 5 
                  AND u.StateId = @StateId
                  AND u.IsActive = 1";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<AvailableSpecialistDto>(sql, new { StateId = stateId });
        }

        public async Task<IEnumerable<AvailableSpecialistDto>> GetAllSpecialistsByCoe(int coeId)
        {
            var sql = @"
                SELECT 
                    u.UserId,
                    CONCAT(u.Title, ' ', u.FirstName, ' ', IFNULL(u.LastName, '')) AS FullName,
                    u.Speciality,
                    c.CoeName
                FROM Users u
                INNER JOIN CentersOfExcellence c ON u.CoeId = c.CoeId
                WHERE u.RoleId = 5
                  AND u.CoeId = @CoeId
                  AND u.IsActive = 1";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<AvailableSpecialistDto>(sql, new { CoeId = coeId });
        }
    }
}
