using TeleICU.API.DTOs.SpecialistDtos;
using TeleICU.API.Helpers;
using TeleICU.API.Models.AuthModels;
using TeleICU.API.Models.SpecialistModels;
using TeleICU.API.Repository.Interface;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Services
{
    public class SpecialistService : ISpecialistService
    {
        private readonly ISpecialistRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly EncounterStorageService _encounterStorage;
        //   private readonly RedisCacheHelper _cache;

        public SpecialistService(ISpecialistRepository repo/*, RedisCacheHelper cache*/, IAuthRepository authRepo, EncounterStorageService encounterStorage)
        {
            _repo = repo;
            _authRepo = authRepo;
            _encounterStorage = encounterStorage;
            //  _cache = cache;
        }

        public async Task<IEnumerable<SpecialistDto>> GetByCoeId(int coeId)
        {
            //string cacheKey = $"specialists:coe:{coeId}";
            //var cached = await _cache.GetAsync<IEnumerable<SpecialistDto>>(cacheKey);
            //if (cached != null) return cached;
            var specialists = await _repo.GetByCoeId(coeId);
            // await _cache.SetAsync(cacheKey, specialists, TimeSpan.FromMinutes(10));
            return specialists;
        }

        public async Task<bool> AssignSpecialist(AssignSpecialistDto dto)
        {
            var specialist = new SpecialistModel
            {
                UserId = dto.UserId,
                CoeId = dto.CoeId
            };
            var result = await _repo.AssignSpecialist(specialist);
            //if (result)
            //{
            //    await _cache.RemoveAsync($"specialists:coe:{dto.CoeId}");
            //    await _cache.RemoveAsync("specialists:all");
            //}
            return result;
        }

        public async Task<bool> RemoveSpecialist(int userId, int coeId)
        {
            var result = await _repo.RemoveSpecialist(userId, coeId);
            //if (result)
            //{
            //    await _cache.RemoveAsync($"specialists:coe:{coeId}");
            //    await _cache.RemoveAsync("specialists:all");
            //}
            return result;
        }

        public async Task<IEnumerable<AllSpecialistDto>> GetAllSpecialists(int userId, string role)
        {
            //string cacheKey = $"specialists:all:{role}:{userId}";
            //var cached = await _cache.GetAsync<IEnumerable<AllSpecialistDto>>(cacheKey);
            //if (cached != null) return cached;
            var specialists = await _repo.GetAllSpecialists(userId, role);
            //  await _cache.SetAsync(cacheKey, specialists, TimeSpan.FromMinutes(10));
            return specialists;
        }

        public async Task<SpecialistResponseDto> GetSpecialist(int id)
        {
            //string cacheKey = $"specialist:{id}";
            //var cached = await _cache.GetAsync<SpecialistResponseDto>(cacheKey);
            //if (cached != null) return cached;
            var specialist = await _repo.GetSpecialistById(id);
            //if (specialist != null)
            //    await _cache.SetAsync(cacheKey, specialist, TimeSpan.FromMinutes(10));
            return specialist;
        }

        public async Task<IEnumerable<SpecialistSimpleDto>> GetUnmappedSpecialistsByState(int userId)
        {
            var user = await _authRepo.GetById(userId);
            if (user == null || user.StateId == null)
                throw new Exception("User is not assigned to any state.");

            var specialists = await _repo.GetUnmappedSpecialistsByState(user.StateId.Value);

            return specialists.Select(s => new SpecialistSimpleDto
            {
                UserId = s.UserId,
                FullName = s.FullName
            });
        }


        public async Task<IEnumerable<SpecialistSimpleDto>> GetMappedSpecialistsByCoe(int coeId)
        {
            return await _repo.GetMappedSpecialistsByCoe(coeId);
        }


        public async Task<int> MapSpecialistToCoe(int specialistId, int coeId)
        { 
            return await _repo.MapSpecialistToCoe(specialistId, coeId);
        }

        public async Task<int> UnmapSpecialist(int specialistId)
        {
            return await _repo.UnmapSpecialist(specialistId);
        }

        public async Task<IEnumerable<AvailableSpecialistDto>> GetAvailableSpecialists(int userId, string role, int? coeId = null)
        {
            // Get the user's state
            var user = await _authRepo.GetById(userId);
            if (user == null)
                throw new Exception($"User with ID {userId} not found.");
            
            if (user.StateId == null)
                throw new Exception($"User {userId} is not assigned to any state. StateId is null.");

            // Get all specialists based on role and filters
            IEnumerable<AvailableSpecialistDto> specialists;
            
            if (coeId.HasValue)
            {
                // Get specialists for specific COE
                specialists = await _repo.GetAllSpecialistsByCoe(coeId.Value);
            }
            else
            {
                // Get all specialists in the user's state
                specialists = await _repo.GetAllSpecialistsByState(user.StateId.Value);
            }

            // Get online users and users on call from Redis
            var onlineUsers = await _encounterStorage.GetOnlineUsersAsync();
            var usersOnCall = await _encounterStorage.GetUsersOnCallAsync();

            // Map the online and call status
            var result = specialists.Select(s => new AvailableSpecialistDto
            {
                UserId = s.UserId,
                FullName = s.FullName,
                Speciality = s.Speciality ?? "Not Specified",
                CoeName = s.CoeName ?? "Not Assigned",
                IsOnline = onlineUsers.Contains(s.UserId.ToString()),
                IsOnCall = usersOnCall.Contains(s.UserId.ToString())
            });

            return result;
        }

        public class SpecialistSimpleDto
        {
            public int UserId { get; set; }
            public string FullName { get; set; } = string.Empty;
        }

        public class MapSpecialistRequest
        {
            public int SpecialistId { get; set; }
            public int? CoeId { get; set; } // Optional - only used by state admin
        }
    }
}