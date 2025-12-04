using Dapper;
using TeleICU.API.DTOs.SpokeDtos;
using TeleICU.API.DTOs.StateDtos;
using TeleICU.API.Models.AuthModels;
using TeleICU.API.Models.SpokeModels;
using TeleICU.API.Models.StateModels;
using TeleICU.API.Repository.Interface;

namespace TeleICU.API.Repository
{
    public class StateRepository : IStateRepository
    {
        private readonly DapperContext _context;

        public StateRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StateModel>> GetAll()
        {
            var sql = @"
                SELECT 
                    StateId,
                    StateName,
                    StateCode,
                    CreatedBy
                FROM States";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<StateModel>(sql);
        }
        public async Task<IEnumerable<DistrictDto>> GetDistrictsByStateCodeAsync(int stateCode)
        {
            var sql = "SELECT DistrictCode, DistrictName FROM Districts WHERE StateCode = @StateCode ORDER BY DistrictName";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<DistrictDto>(sql, new { StateCode = stateCode });
        }

        public async Task<IEnumerable<CityDto>> GetCitiesByDistrictCodeAsync(int districtCode)
        {
            var sql = "SELECT CityCode, CityName FROM Cities WHERE DistrictCode = @DistrictCode ORDER BY CityName";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<CityDto>(sql, new { DistrictCode = districtCode });
        }
        public async Task<StateModel?> GetById(int id)
        {
            var sql = @"
                SELECT 
                    StateId,
                    StateName,
                    CreatedBy
                FROM States 
                WHERE StateId = @Id";

            using var conn = _context.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<StateModel>(sql, new { Id = id });
        }

        public async Task<bool> UnassignStateAdmin(int stateId)
        {
            var sql = "DELETE FROM StateAdminsMapping WHERE StateId = @StateId";
            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(sql, new { StateId = stateId }) > 0;
        }

        //public async Task<bool> AssignAdmin(int stateId, int userId)
        //{
        //    var sql = @"INSERT IGNORE INTO StateAdminsMapping (StateId, UserId)
        //        VALUES (@StateId, @UserId);";

        //    using var connection = _context.CreateConnection();
        //    var affected = await connection.ExecuteAsync(sql, new { StateId = stateId, UserId = userId });
        //    return affected > 0;
        //}

        public async Task<StateModel?> GetByUserId(int userId)
        {
            var sql = @"
        SELECT s.StateId, s.StateName, s.CreatedBy
        FROM States s
        INNER JOIN StateAdminsMapping sam ON s.StateId = sam.StateId
        WHERE sam.UserId = @UserId";

            using var conn = _context.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<StateModel>(sql, new { UserId = userId });
        }


        //public async Task<IEnumerable<StateNetworkDetailsDto>> GetStateNetworkById(int stateId)
        //{
        //    var sql = @"
        //SELECT 
        //    s.StateId,
        //    s.StateName,
        //    u.FirstName AS AdminName,
        //    COUNT(DISTINCT c.CoeId) AS CoeCount,
        //    COUNT(DISTINCT sp.SpokeId) AS SpokeCount
        //FROM States s
        //INNER JOIN StateAdminsMapping sa ON sa.StateId = s.StateId
        //INNER JOIN Users u ON sa.UserId = u.UserId
        //LEFT JOIN CentersOfExcellence c ON c.StateId = s.StateId
        //LEFT JOIN Spokes sp ON sp.CoeId = c.CoeId
        //WHERE s.StateId = @StateId
        //GROUP BY s.StateId, s.StateName, u.FirstName";

        //    using var conn = _context.CreateConnection();
        //    return (await conn.QueryAsync<StateNetworkDetailsDto>(sql, new { StateId = stateId })).ToList();
        //}

        public async Task<StateAdminResponseDto?> GetAdminByStateId(int stateId)
        {
            var query = @"
             SELECT 
                 u.UserId, u.Username, u.Password, u.RoleId, u.StateId, u.CoeId, u.SpokeId, u.Title,
                 u.FirstName, u.MiddleName, u.LastName,CONCAT_WS(' ', u.Title, u.FirstName, u.MiddleName, u.LastName) AS FullName, u.Gender, u.Dob, u.Language,
                 u.SignaturePath, u.ProfilePic, u.Phone, u.Email, u.AddressLine1, u.AddressLine2,
                 u.Pin, u.FacebookProfile, u.TwitterProfile, u.LinkedInProfile,
                 u.RegistrationNumber, u.Qualification, u.Speciality, u.Experience,
                 u.CreatedBy, u.IsActive, u.CreatedDate, u.UpdatedDate,
             
                 u.StateId, s.StateName, s.StateCode,
                 d.DistrictId, d.DistrictName, d.DistrictCode,
                 c.CityId, c.CityName, c.CityCode
             
             FROM Users u
             LEFT JOIN States s ON u.State = s.StateCode
             LEFT JOIN Districts d ON u.District = d.DistrictCode
             LEFT JOIN Cities c ON u.City = c.CityCode
             WHERE u.StateId = @StateId AND u.RoleId = 2";

            using var conn = _context.CreateConnection();

            var result = await conn.QueryAsync<StateAdminResponseDto, StateDto, DistrictDto, CityDto, StateAdminResponseDto>(
                query,
                (user, state, district, city) =>
                {
                    user.State = state;
                    user.District = district;
                    user.City = city;
                    return user;
                },
                new { StateId = stateId },
                splitOn: "StateId,DistrictId,CityId"
            );

            return result.FirstOrDefault();
        }




        public async Task<IEnumerable<StateModel>> GetAssignedStates()
        {
            var sql = @"
        SELECT DISTINCT s.StateId, s.StateName
        FROM States s
        INNER JOIN StateAdminsMapping sam ON s.StateId = sam.StateId";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<StateModel>(sql);
        }

        public async Task<IEnumerable<StateModel>> GetUnassignedStates()
        {
            var sql = @"
        SELECT s.StateId, s.StateName, s.CreatedBy
        FROM States s
        WHERE NOT EXISTS (
            SELECT 1 FROM StateAdminsMapping sam
            WHERE sam.StateId = s.StateId)";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<StateModel>(sql);
        }
    }
}