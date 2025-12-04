using System.Security.Claims;
using TeleICU.API.DTOs.AuthDtos;
using TeleICU.API.Models.AuthModels;

namespace TeleICU.API.Services.Interface
{
    public interface IAuthService
    {
        Task<bool> RegisterUser(RegisterUserRequest request, ClaimsPrincipal currentUser, HttpRequest requests);
        Task<LoginResponse?> Login(LoginRequest request);
        Task<UserModel> GetProfile(int userId);
        Task<bool> ChangePassword(int userId, ChangePasswordRequest request);
        Task SendOtpToEmail(string email);
        Task<UserModel> GetUserByEmail(string email);
        Task<bool> UpdatePassword(int userId, string newHashedPassword);
        Task SendTempPasswordEmail(string toEmail, string tempPassword);
        Task<bool> UpdateUser(int userId, UpdateUserRequest request, ClaimsPrincipal currentUser, HttpRequest requests);
    }
}