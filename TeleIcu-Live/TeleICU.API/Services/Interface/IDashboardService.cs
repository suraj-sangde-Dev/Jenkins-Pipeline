using TeleICU.API.DTOs.DashBoard;

namespace TeleICU.API.Services.Interface
{
    public interface IDashboardService
    {
        Task<DashboardCountsDto> GetDashboardCounts(int userId, string role);
    }
}
