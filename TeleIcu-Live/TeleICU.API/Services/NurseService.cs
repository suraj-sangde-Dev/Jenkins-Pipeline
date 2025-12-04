using TeleICU.API.DTOs.NurseDtos;
using TeleICU.API.Helpers;
using TeleICU.API.Models.NurseModels;
using TeleICU.API.Repository.Interface;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Services
{
    public class NurseService : INurseService
    {
        private readonly INurseRepository _repo;
        private readonly IAuthRepository _authRepo;
        //  private readonly RedisCacheHelper _cache;

        public NurseService(INurseRepository repo/*, RedisCacheHelper cache*/, IAuthRepository authRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
            // _cache = cache;
        }

        public async Task<IEnumerable<NurseDto>> GetBySpokeId(int spokeId)
        {
            //string cacheKey = $"nurses:spoke:{spokeId}";
            //var cached = await _cache.GetAsync<IEnumerable<NurseDto>>(cacheKey);
            //if (cached != null) return cached;
            var nurses = await _repo.GetBySpokeId(spokeId);
            //   await _cache.SetAsync(cacheKey, nurses, TimeSpan.FromMinutes(10));
            return nurses;
        }

        public async Task<bool> AssignNurse(AssignNurseDto dto)
        {
            var nurse = new NurseModel
            {
                UserId = dto.UserId,
                SpokeId = dto.SpokeId
            };
            var result = await _repo.AssignNurse(nurse);
            //if (result)
            //{
            //    await _cache.RemoveAsync($"nurses:spoke:{dto.SpokeId}");
            //    await _cache.RemoveAsync("nurses:all");
            //}
            return result;
        }

        public async Task<bool> RemoveNurse(int userId, int spokeId)
        {
            var result = await _repo.RemoveNurse(userId, spokeId);
            //if (result)
            //{
            //    await _cache.RemoveAsync($"nurses:spoke:{spokeId}");
            //    await _cache.RemoveAsync("nurses:all");
            //}
            return result;
        }

        public async Task<IEnumerable<NurseDto>> GetAllNursesAsync(int userId, string role)
        {
            if (role == "super_admin")
            {
                return await _repo.GetAllNursesAsync();
            }
            else if (role == "state_admin" || role == "coe_admin")
            {
                var user = await _authRepo.GetById(userId);
                if (user?.StateId == null) return Enumerable.Empty<NurseDto>();
                return await _repo.GetAllNursesByStateAsync(user.StateId.Value);
            }
            else if (role == "spoke_admin")
            {
                var user = await _authRepo.GetById(userId);
                if (user?.SpokeId == null) return Enumerable.Empty<NurseDto>();
                return await _repo.GetAllNursesBySpokeAsync(user.SpokeId.Value);
            }
            else
            {
                return Enumerable.Empty<NurseDto>();
            }
        }

        public async Task<NurseResponseDto> GetNurse(int id)
        {
            //string cacheKey = $"nurse:{id}";
            //var cached = await _cache.GetAsync<NurseResponseDto>(cacheKey);
            //if (cached != null) return cached;
            var nurse = await _repo.GetNurseById(id);
            //if (nurse != null)
            //    await _cache.SetAsync(cacheKey, nurse, TimeSpan.FromMinutes(10));
            return nurse;
        }

        public async Task<IEnumerable<NurseDetailsDto>> GetNurseByCaseId(int caseId)
        {
            var nurse = await _repo.GetNurseByCaseId(caseId);
            return nurse;
        }


        public async Task<IEnumerable<NurseSimpleDto>> GetUnmappedNursesByState(int userId)
        {
            var user = await _authRepo.GetById(userId);
            if (user == null || user.StateId == null)
                throw new Exception("User is not assigned to any state.");

            var nurses = await _repo.GetUnmappedNursesByState(user.StateId.Value);

            return nurses.Select(n => new NurseSimpleDto
            {
                UserId = n.UserId,
                FullName = n.FullName
            });
        }
        
        public async Task<IEnumerable<NurseSimpleDto>> GetMappedNursesBySpoke(int spokeId)
        {
            return await _repo.GetMappedNursesBySpoke(spokeId);
        }

        public async Task<int> MapNurseToSpoke(int nurseId, int spokeId)
        {
            return await _repo.MapNurseToSpoke(nurseId, spokeId);
        }

        public async Task<int> UnmapNurse(int nurseId)
        {
            return await _repo.UnmapNurse(nurseId);
        }

        public class NurseSimpleDto
        {
            public int UserId { get; set; }
            public string FullName { get; set; } = string.Empty;
        }

        public class MapNurseRequest
        {
            public int NurseId { get; set; }
            public int? SpokeId { get; set; } // Optional - only used by state admin
        }


    }
}