using TeleICU.API.DTOs.DashBoard;
using TeleICU.API.Helpers;
using TeleICU.API.Repository.Interface;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _repo;
      //  private readonly RedisCacheHelper _cache;

        public DashboardService(IDashboardRepository repo/*, RedisCacheHelper cache*/)
        {
            _repo = repo;
        //    _cache = cache;
        }

        public async Task<DashboardCountsDto> GetDashboardCounts(int userId, string role)
        {
            //string cacheKey = $"dashboard:counts:{role}:{userId}";
            //var cached = await _cache.GetAsync<DashboardCountsDto>(cacheKey);
            //if (cached != null) return cached;
            var counts = await _repo.GetDashboardCounts(userId, role);
          //  await _cache.SetAsync(cacheKey, counts, TimeSpan.FromMinutes(5));
            return counts;
        }
    }
}
