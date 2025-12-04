using Dapper;
using TeleICU.API.Models.AuthModels;
using TeleICU.API.Repository.Interface;

namespace TeleICU.API.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DapperContext _context;
        public AuthRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<UserModel> GetByUsername(string username)
        {
            var sql = @"SELECT UserId, Username, Password, Email, Phone,
                FirstName, MiddleName, LastName, RoleId, StateId, ProfilePic,
                Title, Gender, DOB, Language, SignaturePath,
                AddressLine1, AddressLine2, State, District, City, PIN,
                FacebookProfile, TwitterProfile, LinkedInProfile,
                RegistrationNumber, Qualification, Speciality, Experience,
                CreatedBy, IsActive, CreatedDate, UpdatedDate
                FROM Users WHERE Username = @Username";

            using var connection = _context.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<UserModel>(sql, new { Username = username });
        }

        public async Task<UserModel> GetById(int userId)
        {
            var sql = @"SELECT UserId, Username, Email, Phone,
                FirstName, MiddleName, LastName, RoleId, StateId, ProfilePic,
                Title, Gender, DOB, Language, SignaturePath,
                AddressLine1, AddressLine2, State, District, City, PIN,
                FacebookProfile, TwitterProfile, LinkedInProfile,
                RegistrationNumber, Qualification, Speciality, Experience,
                CreatedBy, IsActive, CreatedDate, UpdatedDate,CoeId,SpokeId
                FROM Users WHERE UserId = @UserId";

            using var connection = _context.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<UserModel>(sql, new { UserId = userId });
        }

        public async Task<UserModel?> GetByEmail(string email)
        {
            var sql = @"
                        SELECT 
                            u.*,
                            s.StateName AS StateName,
s.StateCode AS StateCode,
                            c.CoeName AS CoeName,
                            sp.SpokeHospitalName AS SpokeHospitalName
                        FROM Users u
                        LEFT JOIN States s ON u.StateId = s.StateId
                        LEFT JOIN CentersOfExcellence c ON u.CoeId = c.CoeId
                        LEFT JOIN Spokes sp ON u.SpokeId = sp.SpokeId
                        WHERE u.Email = @Email";

            using var connection = _context.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<UserModel>(sql, new { Email = email });
        }


        public async Task<bool> UpdatePassword(int userId, string newHashedPassword)
        {
            var sql = "UPDATE Users SET Password = @Password WHERE UserId = @UserId";

            using var connection = _context.CreateConnection();
            var rows = await connection.ExecuteAsync(sql, new { Password = newHashedPassword, UserId = userId });
            return rows > 0;
        }

        public async Task<bool> UpdateUser(int userId, UserModel user)
        {
            var sql = @"
                UPDATE Users SET 
                    Phone = @Phone,
                    Title = @Title,
                    FirstName = @FirstName,
                    MiddleName = @MiddleName,
                    LastName = @LastName,
                    Gender = @Gender,
                    DOB = @DOB,
                    Language = @Language,
                    SignaturePath = @SignaturePath,
                    ProfilePic = @ProfilePic,
                    AddressLine1 = @AddressLine1,
                    AddressLine2 = @AddressLine2,
                    State = @State,
                    District = @District,
                    City = @City,
                    PIN = @PIN,
                    FacebookProfile = @FacebookProfile,
                    TwitterProfile = @TwitterProfile,
                    LinkedInProfile = @LinkedInProfile,
                    RegistrationNumber = @RegistrationNumber,
                    Qualification = @Qualification,
                    Speciality = @Speciality,
                    Experience = @Experience,
                    IsActive = @IsActive,
                    UpdatedDate = @UpdatedDate
                WHERE UserId = @UserId";

            using var connection = _context.CreateConnection();
            var rows = await connection.ExecuteAsync(sql, new { 
                userId,
                user.Phone,
                user.Title,
                user.FirstName,
                user.MiddleName,
                user.LastName,
                user.Gender,
                user.DOB,
                user.Language,
                user.SignaturePath,
                user.ProfilePic,
                user.AddressLine1,
                user.AddressLine2,
                user.State,
                user.District,
                user.City,
                user.PIN,
                user.FacebookProfile,
                user.TwitterProfile,
                user.LinkedInProfile,
                user.RegistrationNumber,
                user.Qualification,
                user.Speciality,
                user.Experience,
                user.IsActive,
                UpdatedDate = DateTime.UtcNow
            });
            return rows > 0;
        }

        public async Task<bool> CreateUser(UserModel user, UserRole creatorRole)
        {
            var sqlUser = @"
        INSERT INTO Users (
            Username, Password, RoleId, Email, Phone, StateId, CoeId, SpokeId, ProfilePic,
            Title, FirstName, MiddleName, LastName, Gender, DOB, Language, SignaturePath,
            AddressLine1, AddressLine2, State, District, City, PIN,
            FacebookProfile, TwitterProfile, LinkedInProfile,
            RegistrationNumber, Qualification, Speciality, Experience,
            CreatedBy, IsActive, CreatedDate, UpdatedDate
        )
        VALUES (
            @Username, @Password, @RoleId, @Email, @Phone, @StateId, @CoeId, @SpokeId, @ProfilePic,
            @Title, @FirstName, @MiddleName, @LastName, @Gender, @DOB, @Language, @SignaturePath,
            @AddressLine1, @AddressLine2, @State, @District, @City, @PIN,
            @FacebookProfile, @TwitterProfile, @LinkedInProfile,
            @RegistrationNumber, @Qualification, @Speciality, @Experience,
            @CreatedBy, @IsActive, @CreatedDate, @UpdatedDate
        );
        SELECT LAST_INSERT_ID();";

            using var conn = _context.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction(); 
            try
            {
                switch (user.RoleId)
                {
                    case UserRole.state_admin:
                        if (user.StateId == null)
                            throw new ArgumentException("StateId is required for State Admin.");
                        break;

                    case UserRole.coe_admin:
                        if (creatorRole != UserRole.state_admin && user.CoeId == null)
                            throw new ArgumentException("CoeId is required for CoE Admin.");
                        break;

                    case UserRole.spoke_admin:
                        if (creatorRole != UserRole.state_admin && user.SpokeId == null)
                            throw new ArgumentException("SpokeId is required for Spoke Admin.");
                        break;

                    case UserRole.specialist:
                        if (creatorRole != UserRole.state_admin && user.CoeId == null)
                            throw new ArgumentException("CoeId is required for Specialist.");
                        break;

                    case UserRole.doctor:
                    case UserRole.nurse:
                        if (creatorRole != UserRole.state_admin && user.SpokeId == null)
                            throw new ArgumentException($"{user.RoleId} must have SpokeId.");
                        break;
                }

                // ✅ Insert into Users table
                var createdUserId = await conn.ExecuteScalarAsync<int>(sqlUser, user, transaction);

                // 🔗 Role-specific Mapping (only if IDs are provided)
                if (user.RoleId == UserRole.state_admin && user.StateId != null)
                {
                    await conn.ExecuteAsync("INSERT INTO StateAdminsMapping (StateId, UserId) VALUES (@StateId, @UserId)",
                        new { StateId = user.StateId, UserId = createdUserId }, transaction);
                }
                else if (user.RoleId == UserRole.coe_admin && user.CoeId != null)
                {
                    await conn.ExecuteAsync("INSERT INTO CoeAdminsMapping (CoeId, UserId) VALUES (@CoeId, @UserId)",
                        new { CoeId = user.CoeId, UserId = createdUserId }, transaction);
                }
                else if (user.RoleId == UserRole.spoke_admin && user.SpokeId != null)
                {
                    await conn.ExecuteAsync("INSERT INTO SpokeAdminsMapping (SpokeId, UserId) VALUES (@SpokeId, @UserId)",
                        new { SpokeId = user.SpokeId, UserId = createdUserId }, transaction);
                }
                else if (user.RoleId == UserRole.specialist && user.CoeId != null)
                {
                    await conn.ExecuteAsync("INSERT INTO SpecialistsMapping (CoeId, UserId) VALUES (@CoeId, @UserId)",
                        new { CoeId = user.CoeId, UserId = createdUserId }, transaction);
                }
                else if (user.RoleId == UserRole.doctor && user.SpokeId != null)
                {
                    await conn.ExecuteAsync("INSERT INTO Doctor (SpokeId, UserId) VALUES (@SpokeId, @UserId)",
                        new { SpokeId = user.SpokeId, UserId = createdUserId }, transaction);
                }
                else if (user.RoleId == UserRole.nurse && user.SpokeId != null)
                {
                    await conn.ExecuteAsync("INSERT INTO Nurses (SpokeId, UserId) VALUES (@SpokeId, @UserId)",
                        new { SpokeId = user.SpokeId, UserId = createdUserId }, transaction);
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}