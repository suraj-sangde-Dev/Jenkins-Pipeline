using TeleICU.API.DTOs.DoctorDtos;
using TeleICU.API.DTOs.SpokeDtos;
using TeleICU.API.Helpers;
using TeleICU.API.Repository.Interface;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Services
{
    public class BedsService : IBedsService
    {
        private readonly IBedsRepository _repo;
        //  private readonly RedisCacheHelper _cache;

        public BedsService(IBedsRepository repo)
        {
            _repo = repo;
            //    _cache = cache;
        }
        public async Task<IEnumerable<BedsDto>> GetBeds(int spokeId)
        {
            var bedlist = await _repo.GetBeds(spokeId);
            //  await _cache.SetAsync(cacheKey, doctors, TimeSpan.FromMinutes(10));
            return bedlist;
        }
    }
}

