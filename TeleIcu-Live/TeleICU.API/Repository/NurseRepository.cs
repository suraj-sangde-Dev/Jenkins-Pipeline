using Dapper;
using TeleICU.API.DTOs.NurseDtos;
using TeleICU.API.DTOs.SpecialistDtos;
using TeleICU.API.DTOs.StateDtos;
using TeleICU.API.Models.NurseModels;
using TeleICU.API.Repository.Interface;
using static TeleICU.API.Services.NurseService;

namespace TeleICU.API.Repository
{
    public class NurseRepository : INurseRepository
    {
        private readonly DapperContext _context;

        public NurseRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<NurseDto>> GetBySpokeId(int spokeId)
        {
            var sql = @"
                SELECT n.NurseId, n.UserId, n.SpokeId,
                       CONCAT(u.FirstName, ' ', u.MiddleName, ' ', u.LastName) AS FullName,
                       u.Email
                FROM Nurses n
                INNER JOIN Users u ON u.UserId = n.UserId
                WHERE n.SpokeId = @SpokeId";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<NurseDto>(sql, new { SpokeId = spokeId });
        }

        public async Task<bool> AssignNurse(NurseModel nurse)
        {
            var sql = "INSERT IGNORE INTO Nurses (UserId, SpokeId) VALUES (@UserId, @SpokeId)";
            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(sql, nurse) > 0;
        }

        public async Task<bool> RemoveNurse(int userId, int spokeId)
        {
            var sql = "DELETE FROM Nurses WHERE UserId = @UserId AND SpokeId = @SpokeId";
            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(sql, new { UserId = userId, SpokeId = spokeId }) > 0;
        }

        // Repository/NurseRepository.cs
        public async Task<IEnumerable<NurseDto>> GetAllNursesAsync()
        {
            var sql = @"
        SELECT 
            u.UserId,
            CONCAT_WS(' ', u.Title, u.FirstName, u.MiddleName, u.LastName) AS FullName,
            u.Qualification,
            u.Speciality,
            u.RegistrationNumber,
            u.Phone AS Contact,
            u.CreatedDate,
            u.IsActive,
            s.SpokeHospitalName
        FROM Nurses n
        INNER JOIN Users u ON n.UserId = u.UserId
        INNER JOIN Spokes s ON n.SpokeId = s.SpokeId
        WHERE u.RoleId = 7";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<NurseDto>(sql);
        }

        public async Task<IEnumerable<NurseDto>> GetAllNursesByStateAsync(int stateId)
        {
            var sql = @"
        SELECT 
            u.UserId,
            CONCAT_WS(' ', u.Title, u.FirstName, u.MiddleName, u.LastName) AS FullName,
            u.Qualification,
            u.Speciality,
            u.RegistrationNumber,
            u.Phone AS Contact,
            u.CreatedDate,
            u.IsActive,
            s.SpokeHospitalName
        FROM Nurses n
        INNER JOIN Users u ON n.UserId = u.UserId
        INNER JOIN Spokes s ON n.SpokeId = s.SpokeId
        WHERE u.RoleId = 7 AND u.StateId = @StateId";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<NurseDto>(sql, new { StateId = stateId });
        }

        public async Task<IEnumerable<NurseDto>> GetAllNursesBySpokeAsync(int spokeId)
        {
            var sql = @"
        SELECT 
            u.UserId,
            CONCAT_WS(' ', u.Title, u.FirstName, u.MiddleName, u.LastName) AS FullName,
            u.Qualification,
            u.Speciality,
            u.RegistrationNumber,
            u.Phone AS Contact,
            u.CreatedDate,
            u.IsActive,
            s.SpokeHospitalName
        FROM Nurses n
        INNER JOIN Users u ON n.UserId = u.UserId
        INNER JOIN Spokes s ON n.SpokeId = s.SpokeId
        WHERE u.RoleId = 7 AND n.SpokeId = @SpokeId";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<NurseDto>(sql, new { SpokeId = spokeId });
        }

        public async Task<NurseResponseDto> GetNurseById(int id)
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
                u.Experience,
                u.CreatedBy,
                u.IsActive,
                u.CreatedDate
            FROM Users u
            INNER JOIN Nurses sm ON u.UserId = sm.UserId
            INNER JOIN Spokes c ON sm.SpokeId = c.SpokeId
            LEFT JOIN States s ON u.State = s.StateCode
            LEFT JOIN Districts d ON u.District = d.DistrictCode
            LEFT JOIN Cities ci ON u.City = ci.CityCode
            WHERE u.UserId = @UserId AND u.RoleId = 7";

            using var connection = _context.CreateConnection();
            var result = await connection.QueryAsync<NurseResponseDto, StateDto, DistrictDto, CityDto, NurseResponseDto>(
                sql,
                (nurse, state, district, city) =>
                {
                    nurse.State = state;
                    nurse.District = district;
                    nurse.City = city;
                    return nurse;
                },
                new { UserId = id },
                splitOn: "StateId,DistrictId,CityId"
            );
            return result.FirstOrDefault();
        }

        public async Task<IEnumerable<NurseDetailsDto>> GetNurseByCaseId(int caseId)
        {
            var sql = @"
            SELECT 
            u.FirstName, u.LastName, s.SpokeHospitalName, u.RoleId FROM Users u 
            INNER JOIN Spokes s 
            ON u.SpokeId = s.SpokeId
            INNER JOIN Patients p
            ON p.SpokeId = u.SpokeId
            INNER JOIN Cases c
            ON c.PatientId = p.PatientId
            where u.RoleId = 7
            and c.CaseId = @caseId";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<NurseDetailsDto>(sql, new { CaseId = caseId });
        }


        public async Task<IEnumerable<NurseSimpleDto>> GetUnmappedNursesByState(int stateId)
        {
            var sql = @"
                      SELECT 
                          u.UserId,
                          CONCAT(u.FirstName, ' ', IFNULL(u.LastName, '')) AS FullName
                      FROM Users u
                      WHERE u.RoleId = 7 
                        AND u.StateId = @StateId
                        AND (u.SpokeId IS NULL OR u.SpokeId = 0)";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<NurseSimpleDto>(sql, new { StateId = stateId });
        }

        public async Task<IEnumerable<NurseSimpleDto>> GetMappedNursesBySpoke(int spokeId)
        {
            var sql = @"
                      SELECT 
                          u.UserId,
                          CONCAT(u.FirstName, ' ', IFNULL(u.LastName, '')) AS FullName
                      FROM Users u
                      WHERE u.RoleId = 7
                        AND u.SpokeId = @SpokeId";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<NurseSimpleDto>(sql, new { SpokeId = spokeId });
        }

        public async Task<int> MapNurseToSpoke(int nurseId, int spokeId)
        {
            var sql = @"UPDATE Users SET SpokeId = @SpokeId WHERE UserId = @NurseId AND RoleId = 7";
            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(sql, new { NurseId = nurseId, SpokeId = spokeId });
        }

        public async Task<int> UnmapNurse(int nurseId)
        {
            var sql = @"UPDATE Users SET SpokeId = NULL WHERE UserId = @NurseId AND RoleId = 7";
            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(sql, new { NurseId = nurseId });
        }

    }
}
