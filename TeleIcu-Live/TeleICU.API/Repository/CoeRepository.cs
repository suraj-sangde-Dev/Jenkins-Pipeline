using Dapper;
using TeleICU.API.Models.CoeModels;
using TeleICU.API.Repository.Interface;

namespace TeleICU.API.Repository
{
    public class CoeRepository : ICoeRepository
    {
        private readonly DapperContext _context;

        public CoeRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CenterOfExcellenceModel>> GetAll()
        {
            var sql = @"
        SELECT 
            c.CoeId, c.CoeName, c.CoeCode, c.StateId, s.StateName,
            c.Phone, c.Email, 
            c.AddressLine1, c.AddressLine2, 
            c.District, c.City, c.PIN, 
            c.CreatedBy, c.IsActive, c.CreatedDate, c.UpdatedDate, c.CoePicture,
            d.DistrictCode, d.DistrictName,
            ct.CityCode, ct.CityName,
            cam.UserId
        FROM CentersOfExcellence c
        INNER JOIN States s ON c.StateId = s.StateId
        LEFT JOIN Districts d ON c.District = d.DistrictCode
        LEFT JOIN Cities ct ON c.City = ct.CityCode
        LEFT JOIN CoeAdminsMapping cam ON c.CoeId = cam.CoeId";
            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<CenterOfExcellenceModel>(sql);
        }

        public async Task<CenterOfExcellenceModel?> GetById(int id)
        {
            var sql = @"
        SELECT 
            c.CoeId, c.CoeName, c.CoeCode, c.StateId, 
            s.StateName,
            c.Phone, c.Email,
            c.AddressLine1, c.AddressLine2, 
            c.District, c.City, c.PIN, 
            c.CreatedBy, c.IsActive, c.CreatedDate, c.UpdatedDate, c.CoePicture,
            d.DistrictCode, d.DistrictName,
            ct.CityCode, ct.CityName,
            cam.UserId
        FROM CentersOfExcellence c
        JOIN States s ON c.StateId = s.StateId
        LEFT JOIN Districts d ON c.District = d.DistrictCode
        LEFT JOIN Cities ct ON c.City = ct.CityCode
        LEFT JOIN CoeAdminsMapping cam ON c.CoeId = cam.CoeId
        WHERE c.CoeId = @Id";

            using var conn = _context.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<CenterOfExcellenceModel>(sql, new { Id = id });
        }


        public async Task<bool> Create(CenterOfExcellenceModel coe)
        {
            var sql = @"
                INSERT INTO CentersOfExcellence 
                (CoeName, CoeCode, StateId, CoePicture ,Phone, Email, 
                 AddressLine1, AddressLine2, District, City, PIN, IsActive, CreatedBy)
                VALUES 
                (@CoeName, @CoeCode, @StateId,@CoePicture, @Phone, @Email,
                 @AddressLine1, @AddressLine2, @District, @City,  @PIN, @IsActive, @CreatedBy)";

            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(sql, coe) > 0;
        }

        public async Task<bool> Update(CenterOfExcellenceModel coe)
        {
            var sql = @"
                UPDATE CentersOfExcellence 
                SET 
                    CoeName = @CoeName,
                    CoeCode = @CoeCode,
                    StateId = @StateId,
                    Phone = @Phone,
                    Email = @Email,
                    CoePicture= @CoePicture,
                   
                    AddressLine1 = @AddressLine1,
                    AddressLine2 = @AddressLine2,
                    District = @District,
                    City = @City,
                    PIN = @PIN,
                    IsActive = @IsActive,
                    UpdatedDate = CURRENT_TIMESTAMP
                WHERE CoeId = @CoEId";

            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(sql, coe) > 0;
        }

        public async Task<bool> Delete(int id)
        {
            using var conn = _context.CreateConnection();
             conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                // 1. Unmap all Spokes related to this CoE
                var unmapSpokesSql = "UPDATE Spokes SET CoeId = NULL WHERE CoeId = @Id";
                await conn.ExecuteAsync(unmapSpokesSql, new { Id = id }, transaction);

                // 2. Soft delete the CoE (set IsActive = false)
                var softDeleteSql = "UPDATE CentersOfExcellence SET IsActive = 0 WHERE CoeId = @Id";
                var affected = await conn.ExecuteAsync(softDeleteSql, new { Id = id }, transaction);

                transaction.Commit();
                return affected > 0;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<CenterOfExcellenceModel>> GetByStateId(int stateId)
        {
            var sql = @"
        SELECT 
            c.CoeId, c.CoeName, c.CoeCode, c.StateId, 
            s.StateName,
            c.Phone, c.Email, 
            c.AddressLine1, c.AddressLine2, 
            c.District, c.City, c.PIN, 
            c.CreatedBy, c.IsActive, c.CreatedDate, c.UpdatedDate, c.CoePicture,
            d.DistrictCode, d.DistrictName,
            ct.CityCode, ct.CityName,
            cam.UserId
        FROM CentersOfExcellence c
        JOIN States s ON c.StateId = s.StateId
        LEFT JOIN Districts d ON c.District = d.DistrictCode
        LEFT JOIN Cities ct ON c.City = ct.CityCode
        LEFT JOIN CoeAdminsMapping cam ON c.CoeId = cam.CoeId
        WHERE c.StateId = @StateId";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<CenterOfExcellenceModel>(sql, new { StateId = stateId });
        }

        public async Task<bool> AssignAdmin(int coeId, int userId)
        {
            var sql = @"
                INSERT IGNORE INTO CoeAdminsMapping (CoeId, UserId)
                VALUES (@CoeId, @UserId)";

            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(sql, new { CoeId = coeId, UserId = userId }) > 0;
        }
    }
}