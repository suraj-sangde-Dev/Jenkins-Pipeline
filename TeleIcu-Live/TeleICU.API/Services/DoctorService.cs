using TeleICU.API.DTOs.DoctorDtos;
using TeleICU.API.Helpers;
using TeleICU.API.Models.DoctorModels;
using TeleICU.API.Repository.Interface;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Services
{
    public class DoctorService : IDoctorService
    {
        private readonly IDoctorRepository _repo;
        private readonly IAuthRepository _authRepo;
        //  private readonly RedisCacheHelper _cache;

        public DoctorService(IDoctorRepository repo/*, RedisCacheHelper cache*/, IAuthRepository authRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
            //    _cache = cache;
        }

        public async Task<IEnumerable<DoctorDto>> GetBySpokeId(int spokeId)
        {
            //string cacheKey = $"doctors:spoke:{spokeId}";
            //var cached = await _cache.GetAsync<IEnumerable<DoctorDto>>(cacheKey);
            //if (cached != null) return cached;
            var doctors = await _repo.GetBySpokeId(spokeId);
            //  await _cache.SetAsync(cacheKey, doctors, TimeSpan.FromMinutes(10));
            return doctors;
        }

        public async Task<bool> AssignDoctor(AssignDoctorDto dto)
        {
            var doctor = new DoctorModel
            {
                UserId = dto.UserId,
                SpokeId = dto.SpokeId
            };
            var result = await _repo.AssignDoctor(doctor);
            //if (result)
            //{
            //    await _cache.RemoveAsync($"doctors:spoke:{dto.SpokeId}");
            //    await _cache.RemoveAsync("doctors:all");
            //}
            return result;
        }

        public async Task<bool> RemoveDoctor(int userId, int spokeId)
        {
            var result = await _repo.RemoveDoctor(userId, spokeId);
            //if (result)
            //{
            //    await _cache.RemoveAsync($"doctors:spoke:{spokeId}");
            //    await _cache.RemoveAsync("doctors:all");
            //}
            return result;
        }

        public async Task<IEnumerable<DoctorDto>> GetAllDoctors(int userId, string role)
        {
            if (role == "super_admin")
            {
                return await _repo.GetAllDoctors();
            }
            else if (role == "state_admin" || role == "coe_admin")
            {
                var user = await _authRepo.GetById(userId);
                if (user?.StateId == null) return Enumerable.Empty<DoctorDto>();
                return await _repo.GetAllDoctorsByState(user.StateId.Value);
            }
            else if (role == "spoke_admin")
            {
                var user = await _authRepo.GetById(userId);
                if (user?.SpokeId == null) return Enumerable.Empty<DoctorDto>();
                return await _repo.GetAllDoctorsBySpoke(user.SpokeId.Value);
            }
            else
            {
                return Enumerable.Empty<DoctorDto>();
            }
        }

        public async Task<DoctorResponseDto> GetDoctor(int id)
        {
            //string cacheKey = $"doctor:{id}";
            //var cached = await _cache.GetAsync<DoctorResponseDto>(cacheKey);
            //if (cached != null) return cached;
            var doctor = await _repo.GetDoctorById(id);
            //if (doctor != null)
            //    await _cache.SetAsync(cacheKey, doctor, TimeSpan.FromMinutes(10));
            return doctor;
        }


        public async Task<IEnumerable<DoctorSimpleDto>> GetUnmappedDoctorsByState(int userId)
        {
            var user = await _authRepo.GetById(userId);
            if (user == null || user.StateId == null)
                throw new Exception("User is not assigned to any state.");

            var doctors = await _repo.GetUnmappedDoctorsByState(user.StateId.Value);

            return doctors.Select(n => new DoctorSimpleDto
            {
                UserId = n.UserId,
                FullName = n.FullName
            });
        }

        public async Task<IEnumerable<DoctorSimpleDto>> GetMappedDoctorsBySpoke(int spokeId)
        {
            return await _repo.GetMappedDoctorsBySpoke(spokeId);
        }

        public async Task<int> MapDoctorToSpoke(int doctorId, int spokeId)
        {
            return await _repo.MapDoctorToSpoke(doctorId, spokeId);
        }

        public async Task<int> UnmapDoctor(int doctorId)
        {
            return await _repo.UnmapDoctor(doctorId);
        }

        public class DoctorSimpleDto
        {
            public int UserId { get; set; }
            public string FullName { get; set; } = string.Empty;
        }

        public class MapDoctorRequest
        {
            public int DoctorId { get; set; }
            public int? SpokeId { get; set; } // Optional - only used by state admin
        }
    }
}