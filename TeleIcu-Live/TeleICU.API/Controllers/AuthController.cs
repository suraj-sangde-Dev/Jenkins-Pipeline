using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeleICU.API.DTOs.AuthDtos;
using TeleICU.API.Helpers;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Controllers
{
    [ApiController]
    [Route("auth")]
    [Authorize]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _service;
        private readonly IConfiguration _config;
        private readonly RedisOtpStore _otpStore;
        private readonly RedisCacheHelper _cacheHelper;

        public AuthController(IAuthService service, IConfiguration config, RedisOtpStore otpStore, RedisCacheHelper cacheHelper)
        {
            _service = service;
            _config = config;
            _otpStore = otpStore;
            _cacheHelper = cacheHelper;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(DTOs.AuthDtos.LoginRequest request)
        {
            try
            {
                var result = await _service.Login(request);
                if (result == null)
                    return NotFound(new { message = "invalid credentials", statusCode = 404 });

                return Ok(new { data = result, message = "Login successful.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpGet("profile")]
        public async Task<IActionResult> Profile()
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var user = await _service.GetProfile(userId);

                if (user == null)
                    return NotFound(new { message = "userId not found", statusCode = 404 });

                return Ok(new { data = user, message = "Profile fetched.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            try
            {
                if (request.NewPassword != request.ConfirmPassword)
                    throw new Exception($"new password and confirm password does not match");

                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                bool success = await _service.ChangePassword(userId, request);

                if (!success)
                   return Unauthorized(new { message = "validation failed", statusCode = 401 });

                return Ok(new { message = "Password updated.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [Authorize(Roles = "super_admin,state_admin,spoke_admin,coe_admin")]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserRequest request)
        {
            try
            {
                var success = await _service.RegisterUser(request, User, Request);

                if (!success)
                    return Unauthorized(new { message = "user creation failed or role not allowed", statusCode = 401 });

                return Ok(new { message = "User registered successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserById(int userId)
        {
            try
            {
                var user = await _service.GetProfile(userId);

                if (user == null)
                    return NotFound(new { message = "user id not found", statusCode = 401 });

                return Ok(new { data = user, message = "User fetched successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpPut("update/{userId}")]
        public async Task<IActionResult> UpdateUser(int userId, UpdateUserRequest request)
        {
            try
            {
                var success = await _service.UpdateUser(userId, request, User, Request);

                if (!success)
                    return BadRequest(new { message = "user update failed or permission denied", statusCode = 400 });

                return Ok(new { message = "User updated successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            { 
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userIdClaim))
                {
                    throw new Exception($"Invalid user context");
                }

                var cacheKey = $"auth:token:{userIdClaim}";
                await _cacheHelper.RemoveAsync(cacheKey);

                // Note: Swagger's UI still holds the token client-side until user clears it.
                return Ok(new { message = "Logged out successfully. Token invalidated.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordEmailRequest request)
        {
            try
            {
                var email = request.Email.Trim().ToLower();
                var user = await _service.GetUserByEmail(email);

                if (user == null)
                    return Unauthorized(new { message = "email not registered", statusCode = 401 });

                await _service.SendOtpToEmail(email);
                return Ok(new { message = "OTP sent to email.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [AllowAnonymous]
        [HttpPost("reset-password-using-otp")]
        public async Task<IActionResult> ResetPasswordUsingOtp([FromBody] ResetPasswordOtp request)
        {
            try
            {
                var email = request.Email.Trim().ToLower();
                var otpFromStore = await _otpStore.GetOtpAsync(email);

                if (otpFromStore == null)
                    throw new Exception($"OTP has expired or has not accepted");

                if (otpFromStore != request.Otp)
                    throw new Exception($"Invalid OTP");

                var user = await _service.GetUserByEmail(email);
                if (user == null)
                    return NotFound(new { message = "user not found", statusCode = 404 });

                var success = await _service.UpdatePassword(user.UserId, BCrypt.Net.BCrypt.HashPassword(request.NewPassword));
                if (!success)
                    return BadRequest(new { message = "password reset failed", statusCode = 400 });

                await _otpStore.RemoveOtpAsync(email);
                return Ok(new { message = "Password has been reset successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }

        [AllowAnonymous]
        [HttpPost("reset-using-temp-password")]
        public async Task<IActionResult> ResetUsingTempPassword([FromBody] DTOs.AuthDtos.ResetPasswordRequest request)
        {
            try
            {
                var user = await _service.GetUserByEmail(request.Email);
                if (user == null)
                    return NotFound(new { message = "user not found", statusCode = 404 });

                if (!BCrypt.Net.BCrypt.Verify(request.TempPassword, user.Password))
                    throw new Exception($"temporary password is incorrect");

                var newHashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                var success = await _service.UpdatePassword(user.UserId, newHashedPassword);

                if (!success)
                    return BadRequest(new { message = "password reset failed", statusCode = 400 });

                return Ok(new { message = "Password has been reset successfully.", statusCode = 200 });
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }
        }
    }
}


