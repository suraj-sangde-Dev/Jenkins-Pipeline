using TeleICU.API.DTOs.CoeDtos;
using TeleICU.API.Models.CoeModels;
using TeleICU.API.Repository.Interface;
using TeleICU.API.Services.Interface;
using TeleICU.API.Helpers;
using TeleICU.API.DTOs.StateDtos;

namespace TeleICU.API.Services
{
    public class CoeService : ICoeService
    {
        private readonly ICoeRepository _repo;
        private readonly IAuthRepository _authRepo;
     //   private readonly RedisCacheHelper _cache;
        public CoeService(ICoeRepository repo, IAuthRepository authRepo/*, RedisCacheHelper cache*/)
        {
            _repo = repo;
            _authRepo = authRepo;
          //  _cache = cache;
        }

        public async Task<IEnumerable<CoeDto>> GetAll(int userId, string role)
        {
            //string cacheKey = $"coes:all:{role}:{userId}";
            //var cached = await _cache.GetAsync<IEnumerable<CoeDto>>(cacheKey);
            //if (cached != null) return cached;
            IEnumerable<CenterOfExcellenceModel> coes;
            if (role == "super_admin")
            {
                coes = await _repo.GetAll();
            }
            else if (role == "state_admin")
            {
                var state = await _authRepo.GetById(userId);
                if (state == null || state.StateId == null)
                    return Enumerable.Empty<CoeDto>();
                coes = await _repo.GetByStateId(state.StateId.Value);
            }
            else
            {
                return Enumerable.Empty<CoeDto>();
            }
            var result = coes
        .Where(c => c.IsActive)
        .Select(c => new CoeDto
            {
                CoeId = c.CoeId,
                UserId =c.UserId,
                CoeName = c.CoeName,
                CoeCode = c.CoeCode,
                Phone = c.Phone,
                Email = c.Email,
                AddressLine1 = c.AddressLine1,
                AddressLine2 = c.AddressLine2,
                District = new DistrictDto { 
                    DistrictCode = c.DistrictCode ?? 0, 
                    DistrictName = c.DistrictName ?? c.District 
                },
                City = new CityDto { 
                    CityCode = c.CityCode ?? 0, 
                    CityName = c.CityName ?? c.City 
                },
                PIN = c.PIN,
                StateId = c.StateId,
                StateName = c.StateName,
                CoePicture = c.CoePicture,
                CreatedBy = c.CreatedBy,
                IsActive = c.IsActive,
                CreatedDate = c.CreatedDate,
                UpdatedDate = c.UpdatedDate
            });
          //  await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }

        public async Task<CoeDto?> GetById(int id)
        {
            //string cacheKey = $"coe:{id}";
            //var cached = await _cache.GetAsync<CoeDto>(cacheKey);
            //if (cached != null) return cached;
            var c = await _repo.GetById(id);
            if (c == null) return null;
            var dto = new CoeDto
            {
                CoeId = c.CoeId,
                UserId = c.UserId,
                CoeName = c.CoeName,
                CoeCode = c.CoeCode,
                Phone = c.Phone,
                Email = c.Email,
                AddressLine1 = c.AddressLine1,
                AddressLine2 = c.AddressLine2,
                District = new DistrictDto { 
                    DistrictCode = c.DistrictCode ?? 0, 
                    DistrictName = c.DistrictName ?? c.District 
                },
                City = new CityDto { 
                    CityCode = c.CityCode ?? 0, 
                    CityName = c.CityName ?? c.City 
                },
                PIN = c.PIN,
                StateId = c.StateId,
                StateName = c.StateName,
                CoePicture = c.CoePicture,
                CreatedBy = c.CreatedBy,
                IsActive = c.IsActive,
                CreatedDate = c.CreatedDate,
                UpdatedDate = c.UpdatedDate
            };
          //  await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10));
            return dto;
        }

        public async Task<bool> Create(CreateCoeDto dto, int createdBy, int stateId, HttpRequest request)
        {
            string? imagePath = null;

            // ✅ Upload image if exists
            if (dto.CoePicture != null && dto.CoePicture.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "COE");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueName = $"{Guid.NewGuid()}_{dto.CoePicture.FileName}";
                var fullPath = Path.Combine(uploadsFolder, uniqueName);

                using var stream = new FileStream(fullPath, FileMode.Create);
                await dto.CoePicture.CopyToAsync(stream);

                var baseUrl = $"{request.Scheme}://{request.Host}";
                imagePath = $"{baseUrl}/uploads/COE/{uniqueName}";
            }

            var coe = new CenterOfExcellenceModel
            {
                CoeName = dto.CoeName,
                CoeCode = dto.CoeCode,
                StateId = stateId,
                Phone = dto.Phone,
                Email = dto.Email,
                AddressLine1 = dto.AddressLine1,
                AddressLine2 = dto.AddressLine2,
                District = dto.District,
                City = dto.City,
                PIN = dto.PIN,
                CreatedBy = createdBy,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                CoePicture = imagePath
            };

            var result = await _repo.Create(coe);
            //if (result)
            //{
            //    await _cache.RemoveAsync($"coes:all:super_admin:{createdBy}");
            //    await _cache.RemoveAsync($"coes:all:state_admin:{createdBy}");
            //}
            return result;
        }

        public async Task<bool> Update(int id, CreateCoeDto dto, int stateId, HttpRequest request)
        {
            var existing = await _repo.GetById(id);
            if (existing == null) return false;

            var baseUrl = $"{request.Scheme}://{request.Host}";
            var uploadsPath = Path.Combine("wwwroot", "uploads", "coes");
            Directory.CreateDirectory(uploadsPath);

            // image upload
            if (dto.CoePicture is { Length: > 0 })
            {
                var fileName = $"{Guid.NewGuid()}_{dto.CoePicture.FileName}";
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), uploadsPath, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await dto.CoePicture.CopyToAsync(stream);

                if (!string.IsNullOrEmpty(existing.CoePicture))
                {
                    var oldPath = Path.Combine("wwwroot", existing.CoePicture.Replace(baseUrl, "").TrimStart('/'));
                    if (File.Exists(oldPath)) File.Delete(oldPath);
                }

                existing.CoePicture = $"{baseUrl}/uploads/coes/{fileName}";
            }

            existing.CoeName = dto.CoeName;
            existing.CoeCode = dto.CoeCode;
            existing.StateId = stateId;
            existing.Phone = dto.Phone;
            existing.Email = dto.Email;
            existing.AddressLine1 = dto.AddressLine1;
            existing.AddressLine2 = dto.AddressLine2;
            existing.District = dto.District;
            existing.City = dto.City;
            existing.PIN = dto.PIN;
            existing.UpdatedDate = DateTime.UtcNow;

            var result = await _repo.Update(existing);
            //if (result)
            //{
            //    await _cache.RemoveAsync($"coe:{id}");
            //    await _cache.RemoveAsync($"coes:all:super_admin:{existing.CreatedBy}");
            //    await _cache.RemoveAsync($"coes:all:state_admin:{existing.CreatedBy}");
            //}
            return result;
        }

        public async Task<bool> Delete(int id)
        {
            var result = await _repo.Delete(id);
            //if (result)
            //{
            //    await _cache.RemoveAsync($"coe:{id}");
            //}
            return result;
        }

        public async Task<bool> AssignAdmin(int coeId, int adminId)
        {
            var result = await _repo.AssignAdmin(coeId, adminId);
            //if (result)
            //{
            //    await _cache.RemoveAsync($"coe:{coeId}");
            //}
            return result;
        }

        public async Task<IEnumerable<CoeDto>> GetByStateId(int stateId)
        {
            //string cacheKey = $"coes:state:{stateId}";
            //var cached = await _cache.GetAsync<IEnumerable<CoeDto>>(cacheKey);
            //if (cached != null) return cached;
            var coes = await _repo.GetByStateId(stateId);
            var result = coes.Select(c => new CoeDto
            {
                CoeId = c.CoeId,
                UserId = c.UserId,
                CoeName = c.CoeName,
                CoeCode = c.CoeCode,
                Phone = c.Phone,
                Email = c.Email,
                AddressLine1 = c.AddressLine1,
                AddressLine2 = c.AddressLine2,
                District = new DistrictDto { 
                    DistrictCode = c.DistrictCode ?? 0, 
                    DistrictName = c.DistrictName ?? c.District 
                },
                City = new CityDto { 
                    CityCode = c.CityCode ?? 0, 
                    CityName = c.CityName ?? c.City 
                },
                PIN = c.PIN,
                StateId = c.StateId,
                StateName = c.StateName,
                CoePicture = c.CoePicture,
                CreatedBy = c.CreatedBy,
                IsActive = c.IsActive,
                CreatedDate = c.CreatedDate,
                UpdatedDate = c.UpdatedDate
            });
           // await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }
    }
}