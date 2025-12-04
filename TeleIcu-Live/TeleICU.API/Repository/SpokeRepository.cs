using Dapper;
using System.Data;
using TeleICU.API.DTOs.SpokeDtos;
using TeleICU.API.Models.AuthModels;
using TeleICU.API.Models.SpokeModels;
using TeleICU.API.Repository.Interface;
using static TeleICU.API.Services.SpokeService;

namespace TeleICU.API.Repository
{
    public class SpokeRepository : ISpokeRepository
    {
        private readonly DapperContext _context;
        public SpokeRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SpokeModel>> GetAll()
        {
            var sql = @"
    SELECT 
        s.SpokeId, 
        s.SpokeHospitalName, 
        s.CoeId, 
        c.CoeName,
        s.StateId, 
        st.StateName,
        s.Phone, 
        s.Email,
        s.AddressLine1, 
        s.AddressLine2, 
        s.District, 
        s.City, 
        s.PIN,
        s.IcuBeds,
        s.HduBeds,
        s.OtherBeds,
        s.Beds,
        s.TotalBeds,
        s.SpokePicturePath, 
        s.CreatedBy, 
        s.CreatedDate, 
        s.UpdatedDate,
        d.DistrictCode, d.DistrictName,
        ct.CityCode, ct.CityName,
        sam.UserId
    FROM Spokes s
    LEFT JOIN CentersOfExcellence c ON s.CoeId = c.CoeId
    LEFT JOIN States st ON s.StateId = st.StateId
    LEFT JOIN Districts d ON s.District = d.DistrictCode
    LEFT JOIN Cities ct ON s.City = ct.CityCode
    LEFT JOIN SpokeAdminsMapping sam ON s.SpokeId = sam.SpokeId";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<SpokeModel>(sql);
        }


        public async Task<SpokeModel?> GetById(int id)
        {
            var sql = @"
        SELECT 
            s.SpokeId,
            s.SpokeHospitalName,
            s.CoeId,
            c.CoeName,
            s.StateId,
            st.StateName,                   
            s.Phone,
            s.Email,
            s.AddressLine1,
            s.AddressLine2,
            s.District,
            s.City,
            s.PIN,
            s.IcuBeds,
            s.HduBeds,
            s.OtherBeds,
            s.TotalBeds,
            s.Beds,
            s.SpokePicturePath,
            s.CreatedBy,
            s.CreatedDate,
            s.UpdatedDate,
            d.DistrictCode, d.DistrictName,
            ct.CityCode, ct.CityName,
            sam.UserId
        FROM Spokes s
        LEFT JOIN CentersOfExcellence c ON s.CoeId = c.CoeId
        JOIN States st ON s.StateId = st.StateId
        LEFT JOIN Districts d ON s.District = d.DistrictCode
        LEFT JOIN Cities ct ON s.City = ct.CityCode
        LEFT JOIN SpokeAdminsMapping sam ON s.SpokeId = sam.SpokeId
        WHERE s.SpokeId = @Id";

            using var conn = _context.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<SpokeModel>(sql, new { Id = id });
        }


        public async Task<IEnumerable<SpokeModel>> GetByCoeId(int coeId)
        {
            var sql = @"SELECT SpokeId, SpokeHospitalName, CoeId, StateId, Phone, Email,
                               AddressLine1, AddressLine2, District, City, PIN,
                               SpokePicturePath, CreatedBy, CreatedDate, UpdatedDate
                        FROM Spokes WHERE CoeId = @CoeId";
            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<SpokeModel>(sql, new { CoeId = coeId });
        }

        public async Task<IEnumerable<SpokeModel>> GetByStateId(int stateId)
        {
            var sql = @"
        SELECT 
            s.SpokeId, 
            s.SpokeHospitalName, 
            s.CoeId, 
            c.CoeName,
            s.StateId, 
            st.StateName,
            s.Phone, 
            s.Email,
            s.AddressLine1, 
            s.AddressLine2, 
            s.District, 
            s.City, 
            s.PIN,
            s.IcuBeds,
            s.HduBeds,
            s.OtherBeds,
            s.Beds,
            s.TotalBeds,
            s.SpokePicturePath, 
            s.CreatedBy, 
            s.CreatedDate, 
            s.UpdatedDate,
            d.DistrictCode, d.DistrictName,
            ct.CityCode, ct.CityName,
            sam.UserId
        FROM Spokes s
        LEFT JOIN CentersOfExcellence c ON s.CoeId = c.CoeId
        LEFT JOIN States st ON s.StateId = st.StateId
        LEFT JOIN Districts d ON s.District = d.DistrictCode
        LEFT JOIN Cities ct ON s.City = ct.CityCode
        LEFT JOIN SpokeAdminsMapping sam ON s.SpokeId = sam.SpokeId
        WHERE s.StateId = @StateId";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<SpokeModel>(sql, new { StateId = stateId });
        }

        public async Task<IEnumerable<SpokeModel>> GetBySpokeId(int spokeId)
        {
            var sql = @"
        SELECT 
            s.SpokeId, 
            s.SpokeHospitalName, 
            s.CoeId, 
            c.CoeName,
            s.StateId, 
            st.StateName,
            s.Phone, 
            s.Email,
            s.AddressLine1, 
            s.AddressLine2, 
            s.District, 
            s.City, 
            s.PIN,
            s.IcuBeds,
            s.HduBeds,
            s.OtherBeds,
            s.Beds,
            s.TotalBeds,
            s.SpokePicturePath, 
            s.CreatedBy, 
            s.CreatedDate, 
            s.UpdatedDate,
            d.DistrictCode, d.DistrictName,
            ct.CityCode, ct.CityName,
            sam.UserId
        FROM Spokes s
        LEFT JOIN CentersOfExcellence c ON s.CoeId = c.CoeId
        LEFT JOIN States st ON s.StateId = st.StateId
        LEFT JOIN Districts d ON s.District = d.DistrictCode
        LEFT JOIN Cities ct ON s.City = ct.CityCode
        LEFT JOIN SpokeAdminsMapping sam ON s.SpokeId = sam.SpokeId
        WHERE s.SpokeId = @spokeId";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<SpokeModel>(sql, new { SpokeId = spokeId });
        }

        public async Task<bool> Create(SpokeModel spoke)
        {
            using var conn = _context.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                // 1. Insert Spoke
                var sql = @"INSERT INTO Spokes (
            SpokeHospitalName, CoeId, StateId, Phone, Email,
            AddressLine1, AddressLine2, District, City, PIN,
            SpokePicturePath, CreatedBy,
            IcuBeds, HduBeds, OtherBeds, TotalBeds, Beds
        )  
        VALUES (
            @SpokeHospitalName, @CoeId, @StateId, @Phone, @Email,
            @AddressLine1, @AddressLine2, @District, @City, @PIN,
            @SpokePicturePath, @CreatedBy,
            @IcuBeds, @HduBeds, @OtherBeds, @TotalBeds, @Beds
        );
        SELECT LAST_INSERT_ID();";

                int spokeId = await conn.ExecuteScalarAsync<int>(sql, spoke, transaction);

                // 2. Insert Beds (ICU, HDU, Other, General)
                await InsertBedsAsync(conn, transaction, spokeId, "ICU", spoke.IcuBeds);
                await InsertBedsAsync(conn, transaction, spokeId, "HDU", spoke.HduBeds);
                await InsertBedsAsync(conn, transaction, spokeId, "Other", spoke.OtherBeds);
                await InsertBedsAsync(conn, transaction, spokeId, "General", spoke.Beds);

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }

        private async Task InsertBedsAsync(IDbConnection conn, IDbTransaction transaction, int spokeId, string bedType, int count)
        {
            if (count <= 0) return;

            // Get max existing number for this spoke + bed type
            var sqlMax = @"SELECT MAX(CAST(SUBSTRING(BedNumber, LENGTH(@BedType)+1) AS UNSIGNED))
                   FROM Beds WHERE SpokeId = @SpokeId AND BedType = @BedType";

            int lastNumber = await conn.ExecuteScalarAsync<int?>(sqlMax, new { SpokeId = spokeId, BedType = bedType }, transaction) ?? 0;

            // Insert new beds with sequence numbers
            for (int i = 1; i <= count; i++)
            {
                string bedNumber = $"{bedType}{lastNumber + i}";

                var sqlInsert = @"INSERT INTO Beds (SpokeId, BedType, BedNumber, Status)
                          VALUES (@SpokeId, @BedType, @BedNumber, 'Vacant')";

                await conn.ExecuteAsync(sqlInsert, new
                {
                    SpokeId = spokeId,
                    BedType = bedType,
                    BedNumber = bedNumber
                }, transaction);
            }
        }


        public async Task<bool> Update(SpokeModel spoke)
        {
            using var conn = _context.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                // 1. Update Spoke info
                var sqlUpdate = @"UPDATE Spokes SET
                            SpokeHospitalName = @SpokeHospitalName,
                            CoeId = @CoeId,
                            StateId = @StateId,
                            Phone = @Phone,
                            Email = @Email,
                            AddressLine1 = @AddressLine1,
                            AddressLine2 = @AddressLine2,
                            District = @District,
                            City = @City,
                            PIN = @PIN,
                            IcuBeds = @IcuBeds,
                            HduBeds = @HduBeds,
                            OtherBeds = @OtherBeds,
                            TotalBeds = @TotalBeds,
                            Beds = @Beds,
                            SpokePicturePath = @SpokePicturePath,
                            UpdatedDate = CURRENT_TIMESTAMP
                        WHERE SpokeId = @SpokeId";
                await conn.ExecuteAsync(sqlUpdate, spoke, transaction);

                // 2. Update Bed counts
                await UpdateBedsAsync(conn, transaction, spoke.SpokeId, "ICU", spoke.IcuBeds);
                await UpdateBedsAsync(conn, transaction, spoke.SpokeId, "HDU", spoke.HduBeds);
                await UpdateBedsAsync(conn, transaction, spoke.SpokeId, "Other", spoke.OtherBeds);
                await UpdateBedsAsync(conn, transaction, spoke.SpokeId, "General", spoke.Beds);

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }

        private async Task UpdateBedsAsync(IDbConnection conn, IDbTransaction transaction, int spokeId, string bedType, int newCount)
        {
            // Get current count for this bed type
            var sqlCount = @"SELECT COUNT(*) FROM Beds WHERE SpokeId = @SpokeId AND BedType = @BedType";
            int currentCount = await conn.ExecuteScalarAsync<int>(sqlCount, new { SpokeId = spokeId, BedType = bedType }, transaction);

            if (newCount > currentCount)
            {
                // Add missing beds
                int bedsToAdd = newCount - currentCount;

                var sqlMax = @"SELECT MAX(CAST(SUBSTRING(BedNumber, LENGTH(@BedType)+1) AS UNSIGNED))
                       FROM Beds WHERE SpokeId = @SpokeId AND BedType = @BedType";
                int lastNumber = await conn.ExecuteScalarAsync<int?>(sqlMax, new { SpokeId = spokeId, BedType = bedType }, transaction) ?? 0;

                for (int i = 1; i <= bedsToAdd; i++)
                {
                    string bedNumber = $"{bedType}{lastNumber + i}";
                    var sqlInsert = @"INSERT INTO Beds (SpokeId, BedType, BedNumber, Status)
                              VALUES (@SpokeId, @BedType, @BedNumber, 'Vacant')";
                    await conn.ExecuteAsync(sqlInsert, new { SpokeId = spokeId, BedType = bedType, BedNumber = bedNumber }, transaction);
                }
            }
            else if (newCount < currentCount)
            {
                // Delete extra beds (starting from highest bed number)
                int bedsToRemove = currentCount - newCount;
                var sqlDelete = @"DELETE FROM Beds 
                          WHERE SpokeId = @SpokeId AND BedType = @BedType
                          ORDER BY CAST(SUBSTRING(BedNumber, LENGTH(@BedType)+1) AS UNSIGNED) DESC
                          LIMIT @Limit";
                await conn.ExecuteAsync(sqlDelete, new { SpokeId = spokeId, BedType = bedType, Limit = bedsToRemove }, transaction);
            }

            // If equal, no change needed
        }


        public async Task<bool> Delete(int id)
        {
            var sql = "DELETE FROM Spokes WHERE SpokeId = @Id";
            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(sql, new { Id = id }) > 0;
        }

        public async Task<bool> AssignAdmin(int spokeId, int userId)
        {
            var sql = @"INSERT IGNORE INTO SpokeAdminsMapping (SpokeId, UserId)
                        VALUES (@SpokeId, @UserId)";
            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(sql, new { SpokeId = spokeId, UserId = userId }) > 0;
        }

        public async Task<IEnumerable<UserModel>> GetAdminsBySpokeId(int spokeId)
        {
            var sql = @"SELECT u.* FROM Users u
                        INNER JOIN SpokeAdminsMapping sm ON u.UserId = sm.UserId
                        WHERE sm.SpokeId = @SpokeId";
            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<UserModel>(sql, new { SpokeId = spokeId });
        }

        public async Task<IEnumerable<SpokeModel>> GetByStateAdminUserId(int userId)
        {
            var sql = @"
        SELECT 
            s.SpokeId,
            s.SpokeHospitalName,
            s.CoeId,
            s.StateId,
            st.StateName,
            c.CoeName,
            s.Phone,
            s.Email,
            s.AddressLine1,
            s.AddressLine2,
            s.District,
            s.City,
            s.PIN,
            s.IcuBeds,
            s.HduBeds,
            s.OtherBeds,
            s.Beds,
            s.TotalBeds,
            s.SpokePicturePath,
            s.CreatedBy,
            s.CreatedDate,
            s.UpdatedDate,
            d.DistrictCode, d.DistrictName,
            ct.CityCode, ct.CityName,
            sam.UserId
        FROM Spokes s
        INNER JOIN StateAdminsMapping sam2 ON s.StateId = sam2.StateId
        INNER JOIN States st ON s.StateId = st.StateId
        LEFT JOIN CentersOfExcellence c ON s.CoeId = c.CoeId
        LEFT JOIN Districts d ON s.District = d.DistrictCode
        LEFT JOIN Cities ct ON s.City = ct.CityCode
        LEFT JOIN SpokeAdminsMapping sam ON s.SpokeId = sam.SpokeId
        WHERE sam2.UserId = @UserId";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<SpokeModel>(sql, new { UserId = userId });
        }

        public async Task<IEnumerable<SpokeModel>> GetUnmappedSpokesByStateId(int stateId)
        {
            var sql = @"
        SELECT 
            s.SpokeId,
            s.SpokeHospitalName
        FROM Spokes s
        WHERE s.StateId = @StateId
          AND s.CoeId IS NULL";

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<SpokeModel>(sql, new { StateId = stateId });
        }

        public async Task<bool> MapSpokeWithCoe(int spokeId, int coeId)
        {
            var sql = @"UPDATE Spokes 
                SET CoeId = @CoeId 
                WHERE SpokeId = @SpokeId";

            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(sql, new { SpokeId = spokeId, CoeId = coeId }) > 0;
        }

        public async Task<bool> UnmapSpokeFromCoe(int spokeId)
        {
            var sql = @"UPDATE Spokes 
                SET CoeId = NULL 
                WHERE SpokeId = @SpokeId";

            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(sql, new { SpokeId = spokeId }) > 0;
        }


        public async Task<IEnumerable<GetMappedSpoke>> GetMappedSpokes(int coeId)
        {
            var sql = @"SELECT s.SpokeId, s.SpokeHospitalName AS SpokeName
                        FROM Spokes s
                        WHERE s.CoeId = @CoeId";
            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<GetMappedSpoke>(sql, new { CoeId = coeId });
        }


        public async Task<bool> UpdateBedsOnly(int spokeId, int icuBeds, int hduBeds, int otherBeds)
        {
            using var conn = _context.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                // 1️⃣ Update bed counts in Spokes table (no GeneralBeds now)
                var sqlUpdateCounts = @"UPDATE Spokes SET 
                                    IcuBeds = @IcuBeds, 
                                    HduBeds = @HduBeds, 
                                    OtherBeds = @OtherBeds, 
                                    TotalBeds = (@IcuBeds + @HduBeds + @OtherBeds),
                                    UpdatedDate = CURRENT_TIMESTAMP
                                WHERE SpokeId = @SpokeId";

                await conn.ExecuteAsync(sqlUpdateCounts, new
                {
                    SpokeId = spokeId,
                    IcuBeds = icuBeds,
                    HduBeds = hduBeds,
                    OtherBeds = otherBeds
                }, transaction);

                // 2️⃣ Update individual bed entries (no General type)
                await UpdateBedsAsync(conn, transaction, spokeId, "ICU", icuBeds);
                await UpdateBedsAsync(conn, transaction, spokeId, "HDU", hduBeds);
                await UpdateBedsAsync(conn, transaction, spokeId, "Other", otherBeds);

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }


    }
}