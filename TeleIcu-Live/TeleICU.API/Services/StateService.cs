using StackExchange.Redis;
using TeleICU.API.DTOs.SpokeDtos;
using TeleICU.API.DTOs.StateDtos;
using TeleICU.API.Helpers;
using TeleICU.API.Models.AuthModels;
using TeleICU.API.Repository.Interface;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Services
{
    public class StateService : IStateService
    {
        private readonly IStateRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly RedisCacheHelper _cache;

        public StateService(IStateRepository repo, IAuthRepository authRepo, RedisCacheHelper cache)
        {
            _repo = repo;
            _authRepo = authRepo;
            _cache = cache;
        }

        public async Task<IEnumerable<StateDto>> GetAllStates()
        {
            string cacheKey = $"states:all";
            var cached = await _cache.GetAsync<IEnumerable<StateDto>>(cacheKey);
            if (cached != null) return cached;
            var states = await _repo.GetAll();
            await _cache.SetAsync(cacheKey, states, TimeSpan.FromMinutes(10));
            return states.Select(s => new StateDto
            {
                StateId = s.StateId,
                StateCode = s.StateCode,
                StateName = s.StateName
            });
        }


        public async Task<IEnumerable<DistrictDto>> GetDistrictsAsync(int stateCode)
        {
            string cacheKey = $"districts:state:{stateCode}";
            var cached = await _cache.GetAsync<IEnumerable<DistrictDto>>(cacheKey);
            if (cached != null) return cached;
            var districts = await _repo.GetDistrictsByStateCodeAsync(stateCode);
            await _cache.SetAsync(cacheKey, districts, TimeSpan.FromMinutes(10));
            return districts;
        }

        public async Task<IEnumerable<CityDto>> GetCitiesAsync(int districtCode)
        {
            string cacheKey = $"cities:district:{districtCode}";
            var cached = await _cache.GetAsync<IEnumerable<CityDto>>(cacheKey);
            if (cached != null) return cached;
            var cities = await _repo.GetCitiesByDistrictCodeAsync(districtCode);
            await _cache.SetAsync(cacheKey, cities, TimeSpan.FromMinutes(10));
            return cities;
        }

        public async Task<StateDto?> GetById(int id)
        {
            //string cacheKey = $"state:{id}";
            //var cached = await _cache.GetAsync<StateDto>(cacheKey);
            //if (cached != null) return cached;
            var state = await _repo.GetById(id);
            var dto = state == null ? null : new StateDto { StateId = state.StateId, StateName = state.StateName };
            //if (dto != null)
            //    await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10));
            return dto;
        }

        public async Task<bool> UnassignStateAdmin(int stateId)
        {
            var result = await _repo.UnassignStateAdmin(stateId);
            //if (result)
            //    await _cache.RemoveAsync($"state:{stateId}");
            return result;
        }

        public async Task<StateAdminResponseDto?> GetAdminByStateId(int stateId)
        {
            //string cacheKey = $"state:admin:{stateId}";
            //var cached = await _cache.GetAsync<UserModel>(cacheKey);
            //if (cached != null) return cached;
            var user = await _repo.GetAdminByStateId(stateId);
            //if (user != null)
            //await _cache.SetAsync(cacheKey, user, TimeSpan.FromMinutes(10));
            return user;
        }

        public async Task<IEnumerable<StateDto>> GetAssignedStates()
        {
            //string cacheKey = "states:assigned";
            //var cached = await _cache.GetAsync<IEnumerable<StateDto>>(cacheKey);
            //if (cached != null) return cached;
            var states = await _repo.GetAssignedStates();
            var result = states.Select(s => new StateDto { StateId = s.StateId, StateName = s.StateName });
            //  await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }

        public async Task<IEnumerable<StateDto>> GetUnassignedStates()
        {
            //string cacheKey = "states:unassigned";
            //var cached = await _cache.GetAsync<IEnumerable<StateDto>>(cacheKey);
            //if (cached != null) return cached;
            var states = await _repo.GetUnassignedStates();
            var result = states.Select(s => new StateDto { StateId = s.StateId, StateName = s.StateName });
            // await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }
    }
}