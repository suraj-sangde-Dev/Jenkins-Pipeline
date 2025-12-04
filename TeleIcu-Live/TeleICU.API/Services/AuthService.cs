using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using TeleICU.API.DTOs.AuthDtos;
using TeleICU.API.Helpers;
using TeleICU.API.Models.AuthModels;
using TeleICU.API.Repository.Interface;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;
        private readonly EmailSettings _emailSettings;
        private readonly RedisOtpStore _otpStore;
      //  private readonly RedisCacheHelper _cache;

        public AuthService(IAuthRepository repo, IConfiguration config, IOptions<EmailSettings> emailSettings, RedisOtpStore otpStore/*, RedisCacheHelper cache*/)
        {
            _repo = repo;
            _config = config;
            _emailSettings = emailSettings.Value;
            _otpStore = otpStore;
         //   _cache = cache;
        }

        public async Task<LoginResponse?> Login(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Email) || string.IsNullOrWhiteSpace(request?.Password))
                return null;

            var user = await _repo.GetByEmail(request.Email);
            if (user == null || string.IsNullOrWhiteSpace(user.Password))
                return null;

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                return null;

            var token = JwtTokenHelper.GenerateToken(user, _config);

            // Store latest token per user to enforce single-active-token
            try
            {
                var cache = new RedisCacheHelper(ConnectionMultiplexer.Connect(_config.GetConnectionString("Redis")));
                await cache.SetAsync($"auth:token:{user.UserId}", token, TimeSpan.FromHours(8));
            }
            catch
            {
                // best-effort; don't block login if cache fails
            }

            return new LoginResponse
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                UserId = user.UserId,
                FullName = $"{user.FirstName} {user.MiddleName} {user.LastName}".Replace("  ", " ").Trim(),
                Role = user.RoleId.ToString(),
                StateId = user.StateId,
                StateName = user.StateName,
                StateCode = user.StateCode,
                CoeId= user.CoeId,
                CoeName = user.CoeName,
                SpokeId=user.SpokeId,
                SpokeHospitalName = user.SpokeHospitalName,
                ProfilePic = user.ProfilePic
            };
        }

        public async Task<UserModel> GetProfile(int userId)
        {
            //string cacheKey = $"user:profile:{userId}";
            //var cached = await _cache.GetAsync<UserModel>(cacheKey);
            //if (cached != null) return cached;
            var user = await _repo.GetById(userId);
            //if (user != null)
               // await _cache.SetAsync(cacheKey, user, TimeSpan.FromMinutes(10));
            return user;
        }

        public async Task<bool> ChangePassword(int userId, ChangePasswordRequest request)
        {
            var result = false;
            if (request.NewPassword != request.ConfirmPassword)
                return false;
            var user = await _repo.GetById(userId);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.OldPassword, user.Password))
                return false;
            var newHashed = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            result = await _repo.UpdatePassword(userId, newHashed);
            //if (result)
            //    await _cache.RemoveAsync($"user:profile:{userId}");
            return result;
        }

        public async Task<bool> RegisterUser(RegisterUserRequest request, ClaimsPrincipal currentUser, HttpRequest requests)
        {
            string creatorRoleString = currentUser.FindFirst(ClaimTypes.Role)?.Value ?? "";
            int createdBy = int.Parse(currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            string generatedUsername = await GenerateUniqueUsernameAsync((UserRole)request.RoleId);

            if (!Enum.TryParse(creatorRoleString, out UserRole creatorRole))
                return false;

            if (!IsRoleAssignmentAllowed(creatorRole, (UserRole)request.RoleId))
                return false;
            // 🔐 Load the creator (so we can inherit their context like CoeId/SpokeId)
            var creator = await _repo.GetById(createdBy);
            if (creator == null) return false;

            // ✅ Force scope based on creator’s role
            if (creatorRole == UserRole.state_admin)
            {
                // Always enforce creator's state
                request.StateId = creator.StateId;

                if ((UserRole)request.RoleId == UserRole.coe_admin)
                {
                    // allow CoeId from payload, but must belong to same state
                    if (request.CoeId == null)
                        throw new InvalidOperationException("CoeId must be provided when creating a CoE Admin.");
                }
                else if ((UserRole)request.RoleId == UserRole.spoke_admin)
                {
                    // allow SpokeId from payload, enforce state
                    if (request.SpokeId == null)
                        throw new InvalidOperationException("SpokeId must be provided when creating a Spoke Admin.");
                    request.CoeId = null; // CoeId not directly assigned here
                }
                else
                {
                    // for specialist/doctor/nurse created directly by state admin
                    request.CoeId = null;
                    request.SpokeId = null;
                }
            }
            else if (creatorRole == UserRole.coe_admin)
            {
                // CoE admin can only create Specialists
                if ((UserRole)request.RoleId == UserRole.specialist)
                {
                    request.StateId = creator.StateId;
                    request.CoeId = creator.CoeId;
                    request.SpokeId = null;
                }
            }
            else if (creatorRole == UserRole.spoke_admin)
            {
                // Spoke admin can only create Doctors/Nurses
                if ((UserRole)request.RoleId == UserRole.doctor || (UserRole)request.RoleId == UserRole.nurse)
                {
                    request.StateId = creator.StateId;
                    request.CoeId = creator.CoeId;
                    request.SpokeId = creator.SpokeId;
                }
            }

            string tempPassword = GenerateRandomPassword();
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(tempPassword);

            string signaturePath = null;
            if (request.SignatureFile != null && request.SignatureFile.Length > 0)
            {
                var folder = Path.Combine("wwwroot", "uploads", "signatures");
                Directory.CreateDirectory(folder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.SignatureFile.FileName)}";
                var fullPath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await request.SignatureFile.CopyToAsync(stream);
                }

                var baseUrl = $"{requests.Scheme}://{requests.Host}";
                signaturePath = $"{baseUrl}/uploads/signatures/{fileName}";
            }

            string profilePicPath = null;
            if (request.ProfilePic != null && request.ProfilePic.Length > 0)
            {
                var folder = Path.Combine("wwwroot", "uploads", "ProfilePic");
                Directory.CreateDirectory(folder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.ProfilePic.FileName)}";
                var fullFilePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(fullFilePath, FileMode.Create))
                {
                    await request.ProfilePic.CopyToAsync(stream);
                }

                var baseUrl = $"{requests.Scheme}://{requests.Host}";
                profilePicPath = $"{baseUrl}/uploads/ProfilePic/{fileName}";
            }

            var user = new UserModel
            {
                Username = generatedUsername,
                Password = hashedPassword, 
                RoleId = (UserRole)request.RoleId,
                Email = request.Email,
                Phone = request.Phone,
                StateId = request.StateId,
                CoeId = request.CoeId,
                SpokeId = request.SpokeId,
                Title = request.Title,
                FirstName = request.FirstName,
                MiddleName = request.MiddleName,
                LastName = request.LastName,
                Gender = request.Gender,
                DOB = request.DOB,
                Language = request.Language,
                SignaturePath = signaturePath,
                ProfilePic = profilePicPath,
                AddressLine1 = request.AddressLine1,
                AddressLine2 = request.AddressLine2,
                State = request.State,
                District = request.District,
                City = request.City,
                PIN = request.PIN,
                FacebookProfile = request.FacebookProfile,
                TwitterProfile = request.TwitterProfile,
                LinkedInProfile = request.LinkedInProfile,
                RegistrationNumber = request.RegistrationNumber,
                Qualification = request.Qualification,
                Speciality = request.Speciality,
                Experience = request.Experience,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                IsActive = true,
                CreatedBy = createdBy
            };

            var created = await _repo.CreateUser(user, creatorRole);
            if (!created) return false;
         //   await _cache.RemoveAsync($"user:profile:{createdBy}");
            await SendTempPasswordEmail(request.Email, tempPassword);
            return true;
        }

        private string GenerateRandomPassword()
        {
            return Guid.NewGuid().ToString("N")[..8];
        }

        public async Task SendTempPasswordEmail(string toEmail, string tempPassword)
        {
            var settings = _config.GetSection("EmailSettings").Get<EmailSettings>();

            using var client = new SmtpClient(settings.SmtpServer, settings.Port)
            {
                Credentials = new NetworkCredential(settings.Username, settings.Password),
                EnableSsl = settings.UseSsl
            };

            var message = new MailMessage(settings.From, toEmail)
            {
                Subject = "Your Temporary Password",
                Body = $"Welcome to TeleICU.\n\nYour temporary password is: {tempPassword}\n\nPlease reset your password after first login."
            };

            await client.SendMailAsync(message);
        }

        private bool IsRoleAssignmentAllowed(UserRole creatorRole, UserRole targetRole)
        {
            return creatorRole switch
            {
                UserRole.super_admin => new[] { UserRole.state_admin, UserRole.coe_admin, UserRole.spoke_admin, UserRole.specialist, UserRole.doctor, UserRole.nurse }.Contains(targetRole),
                UserRole.state_admin => new[] { UserRole.coe_admin, UserRole.spoke_admin, UserRole.specialist, UserRole.doctor, UserRole.nurse }.Contains(targetRole),
                UserRole.coe_admin => new [] {UserRole.specialist, UserRole.spoke_admin }.Contains(targetRole),
                UserRole.spoke_admin => new[] { UserRole.doctor, UserRole.nurse }.Contains(targetRole),
                _ => false
            };
        } 

        public async Task SendOtpToEmail(string email)
        {
            var otp = new Random().Next(100000, 999999).ToString();
            await _otpStore.StoreOtpAsync(email, otp, TimeSpan.FromMinutes(10));

            using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.Port)
            {
                Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
                EnableSsl = _emailSettings.UseSsl
            };

            var message = new MailMessage
            {
                From = new MailAddress(_emailSettings.From),
                Subject = "Your OTP for TeleICU Password Reset",
                Body = $"Your OTP is: {otp}. It expires in 10 minutes.",
                IsBodyHtml = false,
            };
            message.To.Add(email);

            await client.SendMailAsync(message);
        }

        public async Task<UserModel> GetUserByEmail(string email)
        {
            //string cacheKey = $"user:email:{email}";
            //var cached = await _cache.GetAsync<UserModel>(cacheKey);
            //if (cached != null) return cached;
            var user = await _repo.GetByEmail(email);
            //if (user != null)
            //    await _cache.SetAsync(cacheKey, user, TimeSpan.FromMinutes(10));
            return user;
        }

        public async Task<bool> UpdatePassword(int userId, string newHashedPassword)
        {
            var result = await _repo.UpdatePassword(userId, newHashedPassword);
            //if (result)
            //    await _cache.RemoveAsync($"user:profile:{userId}");
            return result;
        }

        public async Task<bool> UpdateUser(int userId, UpdateUserRequest request, ClaimsPrincipal currentUser, HttpRequest requests)
        {
            // Check if user exists
            var existingUser = await _repo.GetById(userId);
            if (existingUser == null)
                return false;

            // Check if current user has permission to update this user
            var currentUserRole = currentUser.FindFirst(ClaimTypes.Role)?.Value;
            var currentUserId = int.Parse(currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Only allow users to update their own profile, or admins to update any user
            if (currentUserId != userId && !IsAdminRole(currentUserRole))
                return false;

            // Handle file uploads if provided
            string signaturePath = existingUser.SignaturePath;
            if (request.SignatureFile != null && request.SignatureFile.Length > 0)
            {
                var folder = Path.Combine("wwwroot", "uploads", "signatures");
                Directory.CreateDirectory(folder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.SignatureFile.FileName)}";
                var fullPath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await request.SignatureFile.CopyToAsync(stream);
                }

                var baseUrl = $"{requests.Scheme}://{requests.Host}";
                signaturePath = $"{baseUrl}/uploads/signatures/{fileName}";
            }

            string profilePicPath = existingUser.ProfilePic;
            if (request.ProfilePic != null && request.ProfilePic.Length > 0)
            {
                var folder = Path.Combine("wwwroot", "uploads", "ProfilePic");
                Directory.CreateDirectory(folder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.ProfilePic.FileName)}";
                var fullFilePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(fullFilePath, FileMode.Create))
                {
                    await request.ProfilePic.CopyToAsync(stream);
                }

                var baseUrl = $"{requests.Scheme}://{requests.Host}";
                profilePicPath = $"{baseUrl}/uploads/ProfilePic/{fileName}";
            }

            // Update user model with new values
            var updatedUser = new UserModel
            {
                UserId = userId,
                Username = existingUser.Username,
                Password = existingUser.Password,
                RoleId = existingUser.RoleId,
                Email = existingUser.Email,
                Phone = request.Phone ?? existingUser.Phone,
                StateId = existingUser.StateId,
                CoeId = existingUser.CoeId,
                SpokeId = existingUser.SpokeId,
                Title = request.Title ?? existingUser.Title,
                FirstName = request.FirstName ?? existingUser.FirstName,
                MiddleName = request.MiddleName ?? existingUser.MiddleName,
                LastName = request.LastName ?? existingUser.LastName,
                Gender = request.Gender ?? existingUser.Gender,
                DOB = request.DOB ?? existingUser.DOB,
                Language = request.Language ?? existingUser.Language,
                SignaturePath = signaturePath,
                ProfilePic = profilePicPath,
                AddressLine1 = request.AddressLine1 ?? existingUser.AddressLine1,
                AddressLine2 = request.AddressLine2 ?? existingUser.AddressLine2,
                State = request.State ?? existingUser.State,
                District = request.District ?? existingUser.District,
                City = request.City ?? existingUser.City,
                PIN = request.PIN ?? existingUser.PIN,
                FacebookProfile = request.FacebookProfile ?? existingUser.FacebookProfile,
                TwitterProfile = request.TwitterProfile ?? existingUser.TwitterProfile,
                LinkedInProfile = request.LinkedInProfile ?? existingUser.LinkedInProfile,
                RegistrationNumber = request.RegistrationNumber ?? existingUser.RegistrationNumber,
                Qualification = request.Qualification ?? existingUser.Qualification,
                Speciality = request.Speciality ?? existingUser.Speciality,
                Experience = request.Experience ?? existingUser.Experience,
                CreatedBy = existingUser.CreatedBy,
                IsActive = request.IsActive ?? existingUser.IsActive,
                CreatedDate = existingUser.CreatedDate,
                UpdatedDate = DateTime.UtcNow
            };

            var result = await _repo.UpdateUser(userId, updatedUser);
            return result;
        }

        private bool IsAdminRole(string role)
        {
            return role switch
            {
                "super_admin" => true,
                "state_admin" => true,
                "coe_admin" => true,
                "spoke_admin" => true,
                _ => false
            };
        }

        private async Task<string> GenerateUniqueUsernameAsync(UserRole role)
        {
            string rolePrefix = role.ToString().Replace("_", "").ToUpper();     
            string datePart = DateTime.UtcNow.ToString("yyMMdd");         
            string randomPart = Guid.NewGuid().ToString("N")[..6].ToUpper(); 
            return $"{rolePrefix}{datePart}{randomPart}";
        }
    }
}