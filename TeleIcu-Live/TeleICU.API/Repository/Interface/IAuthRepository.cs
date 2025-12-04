using TeleICU.API.Models.AuthModels;

namespace TeleICU.API.Repository.Interface
{
    public interface IAuthRepository
    {
        Task<bool> CreateUser(UserModel user, UserRole creatorRole);
        Task<UserModel> GetByUsername(string username);
        Task<UserModel> GetById(int userId);
        Task<bool> UpdatePassword(int userId, string newHashedPassword);
        Task<UserModel> GetByEmail(string email);
        Task<bool> UpdateUser(int userId, UserModel user);
    }
}