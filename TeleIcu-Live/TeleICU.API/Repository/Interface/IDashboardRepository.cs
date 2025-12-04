using TeleICU.API.DTOs.DashBoard;

namespace TeleICU.API.Repository.Interface
{
    public interface IDashboardRepository
    {
        Task<DashboardCountsDto> GetDashboardCounts(int userId, string role);
    }

}
