using TeleICU.API.DTOs.SpokeDtos;
using TeleICU.API.DTOs.StateDtos;
using TeleICU.API.Helpers;
using TeleICU.API.Models.AuthModels;
using TeleICU.API.Models.SpokeModels;
using TeleICU.API.Repository;
using TeleICU.API.Repository.Interface;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Services
{
    public class SpokeService : ISpokeService
    {
        private readonly ISpokeRepository _repo;
        private readonly IAuthRepository _authRepo;
        //  private readonly RedisCacheHelper _cache;

        public SpokeService(ISpokeRepository repo, IAuthRepository authRepo/*, RedisCacheHelper cache*/)
        {
            _repo = repo;
            _authRepo = authRepo;
            // _cache = cache;
        }

        public async Task<IEnumerable<SpokeDto>> GetSpokesByRole(int userId, string role)
        {
            // string cacheKey = $"spokes:role:{role}:{userId}";
            //  var cached = await _cache.GetAsync<IEnumerable<SpokeDto>>(cacheKey);
            // if (cached != null) return cached;
            IEnumerable<SpokeModel> spokes;
            if (role == "super_admin")
            {
                spokes = await _repo.GetAll();
            }
            else if (role == "state_admin")
            {
                spokes = await _repo.GetByStateAdminUserId(userId);
            }
            else if (role == "coe_admin" || role == "specialist")
            {
                var user = await _authRepo.GetById(userId);
                if (user?.StateId == null)
                    spokes = Enumerable.Empty<SpokeModel>();
                spokes = await _repo.GetByStateId(user.StateId.Value);
            }
            else if (role == "spoke_admin" || role == "nurse" || role == "doctor")
            {
                var user = await _authRepo.GetById(userId);

                if (user?.SpokeId == null)
                    spokes = Enumerable.Empty<SpokeModel>();
                else
                    spokes = await _repo.GetBySpokeId(user.SpokeId.Value);
            }

            else
            {
                spokes = Enumerable.Empty<SpokeModel>();
            }
            var result = spokes.Select(s => new SpokeDto
            {
                UserId = s.UserId,
                SpokeId = s.SpokeId,
                SpokeHospitalName = s.SpokeHospitalName,
                CoeId = s.CoeId,
                StateName = s.StateName,
                CoeName = s.CoeName,
                StateId = s.StateId,
                Phone = s.Phone,
                Email = s.Email,
                AddressLine1 = s.AddressLine1,
                AddressLine2 = s.AddressLine2,
                District = new DistrictDto
                {
                    DistrictCode = s.DistrictCode ?? 0,
                    DistrictName = s.DistrictName ?? s.District
                },
                City = new CityDto
                {
                    CityCode = s.CityCode ?? 0,
                    CityName = s.CityName ?? s.City
                },
                PIN = s.PIN,
                IcuBeds = s.IcuBeds,
                HduBeds = s.HduBeds,
                OtherBeds = s.OtherBeds,
                Beds = s.Beds,
                TotalBeds = s.TotalBeds,
                SpokePicturePath = s.SpokePicturePath,
                CreatedBy = s.CreatedBy,
                CreatedDate = s.CreatedDate,
                UpdatedDate = s.UpdatedDate
            });
            //  await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }

        public async Task<SpokeDto?> GetById(int id)
        {
            //string cacheKey = $"spoke:{id}";
            //var cached = await _cache.GetAsync<SpokeDto>(cacheKey);
            //if (cached != null) return cached;
            var s = await _repo.GetById(id);
            if (s == null) return null;
            var dto = new SpokeDto
            {
                UserId = s.UserId,
                SpokeId = s.SpokeId,
                SpokeHospitalName = s.SpokeHospitalName,
                CoeId = s.CoeId,
                CoeName = s.CoeName,
                StateId = s.StateId,
                StateName = s.StateName,
                Phone = s.Phone,
                Email = s.Email,
                AddressLine1 = s.AddressLine1,
                AddressLine2 = s.AddressLine2,
                District = new DistrictDto
                {
                    DistrictCode = s.DistrictCode ?? 0,
                    DistrictName = s.DistrictName ?? s.District
                },
                City = new CityDto
                {
                    CityCode = s.CityCode ?? 0,
                    CityName = s.CityName ?? s.City
                },
                PIN = s.PIN,
                IcuBeds = s.IcuBeds,
                HduBeds = s.HduBeds,
                OtherBeds = s.OtherBeds,
                Beds = s.Beds,
                TotalBeds = s.TotalBeds,
                SpokePicturePath = s.SpokePicturePath,
                CreatedBy = s.CreatedBy,
                CreatedDate = s.CreatedDate,
                UpdatedDate = s.UpdatedDate
            };
            // await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10));
            return dto;
        }

        public async Task<bool> Create(CreateSpokeDto dto, int createdBy, HttpRequest request)
        {
            // 1. Get user info
            var user = await _authRepo.GetById(createdBy);
            if (user == null || (user.RoleId != UserRole.state_admin && user.RoleId != UserRole.coe_admin))
                return false;

            // 2. Resolve stateId from user info
            int stateId = user.StateId.Value;

            // 3. Upload image
            string? imagePath = null;
            if (dto.SpokePicturePath != null && dto.SpokePicturePath.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "spokes");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueName = $"{Guid.NewGuid()}_{dto.SpokePicturePath.FileName}";
                var fullPath = Path.Combine(uploadsFolder, uniqueName);

                using var stream = new FileStream(fullPath, FileMode.Create);
                await dto.SpokePicturePath.CopyToAsync(stream);

                var baseUrl = $"{request.Scheme}://{request.Host}";
                imagePath = $"{baseUrl}/uploads/spokes/{uniqueName}";
            }

            var spoke = new SpokeModel
            {
                SpokeHospitalName = dto.SpokeHospitalName,
                CoeId = dto.CoeId,
                StateId = stateId,
                Phone = dto.Phone,
                Email = dto.Email,
                AddressLine1 = dto.AddressLine1,
                AddressLine2 = dto.AddressLine2,
                District = dto.District,
                City = dto.City,
                PIN = dto.PIN,
                IcuBeds = dto.IcuBeds,
                HduBeds = dto.HduBeds,
                OtherBeds = dto.OtherBeds,
                Beds = dto.Beds,
                TotalBeds = dto.IcuBeds + dto.HduBeds + dto.OtherBeds + dto.Beds,
                CreatedBy = createdBy,
                SpokePicturePath = imagePath,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            var result = await _repo.Create(spoke);
            //if (result)
            //    await _cache.RemoveAsync($"spokes:role:super_admin:{createdBy}");
            return result;
        }

        public async Task<bool> Update(int id, CreateSpokeDto dto, HttpRequest request)
        {
            var existingSpoke = await _repo.GetById(id);
            if (existingSpoke == null)
                return false;
            int stateId = existingSpoke.StateId;
            string imagePath = existingSpoke.SpokePicturePath;

            if (dto.SpokePicturePath != null && dto.SpokePicturePath.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "spokes");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{dto.SpokePicturePath.FileName}";
                var fullPath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await dto.SpokePicturePath.CopyToAsync(stream);
                }

                var baseUrl = $"{request.Scheme}://{request.Host}";
                imagePath = $"{baseUrl}/uploads/spokes/{uniqueFileName}";

                //  Delete old file
                if (!string.IsNullOrEmpty(existingSpoke.SpokePicturePath))
                {
                    var oldImagePath = existingSpoke.SpokePicturePath.Replace(baseUrl, "").TrimStart('/');
                    var oldFullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldImagePath);
                    if (System.IO.File.Exists(oldFullPath))
                        System.IO.File.Delete(oldFullPath);
                }
            }

            var spoke = new SpokeModel
            {
                SpokeId = id,
                SpokeHospitalName = dto.SpokeHospitalName,
                CoeId = dto.CoeId,
                StateId = stateId,
                Phone = dto.Phone,
                Email = dto.Email,
                AddressLine1 = dto.AddressLine1,
                AddressLine2 = dto.AddressLine2,
                District = dto.District,
                City = dto.City,
                PIN = dto.PIN,
                IcuBeds = dto.IcuBeds,
                HduBeds = dto.HduBeds,
                OtherBeds = dto.OtherBeds,
                Beds = dto.Beds,
                TotalBeds = dto.IcuBeds + dto.HduBeds + dto.OtherBeds + dto.Beds,
                SpokePicturePath = imagePath,
                CreatedBy = existingSpoke.CreatedBy,
                CreatedDate = existingSpoke.CreatedDate,
                UpdatedDate = DateTime.UtcNow
            };

            var result = await _repo.Update(spoke);
            //if (result)
            //{
            //    await _cache.RemoveAsync($"spoke:{id}");
            //    await _cache.RemoveAsync($"spokes:role:super_admin:{spoke.CreatedBy}");
            //}
            return result;
        }

        public async Task<bool> Delete(int id)
        {
            var result = await _repo.Delete(id);
            //if (result)
            //    await _cache.RemoveAsync($"spoke:{id}");
            return result;
        }

        public async Task<bool> AssignAdmin(int spokeId, int userId)
        {
            var result = await _repo.AssignAdmin(spokeId, userId);
            //if (result)
            //    await _cache.RemoveAsync($"spoke:{spokeId}");
            return result;
        }

        public async Task<IEnumerable<UserModel>> GetAdminsBySpokeId(int spokeId)
        {
            //string cacheKey = $"spoke:admins:{spokeId}";
            //var cached = await _cache.GetAsync<IEnumerable<UserModel>>(cacheKey);
            //if (cached != null) return cached;
            var admins = await _repo.GetAdminsBySpokeId(spokeId);
            // await _cache.SetAsync(cacheKey, admins, TimeSpan.FromMinutes(10));
            return admins;
        }

        public async Task<IEnumerable<SpokeMinimalDto>> GetUnmappedSpokesByState(int userId)
        {
            var user = await _authRepo.GetById(userId);
            if (user == null || user.StateId == null)
                throw new Exception("User is not assigned to any state.");

            var spokes = await _repo.GetUnmappedSpokesByStateId(user.StateId.Value);

            return spokes.Select(s => new SpokeMinimalDto
            {
                SpokeId = s.SpokeId,
                SpokeHospitalName = s.SpokeHospitalName
            });
        }

        public async Task<bool> MapSpokeWithCoe(int spokeId, int coeId)
        {
            return await _repo.MapSpokeWithCoe(spokeId, coeId);
        }

        public async Task<bool> UnmapSpokeFromCoe(int spokeId)
        {
            return await _repo.UnmapSpokeFromCoe(spokeId);
        }
        public async Task<IEnumerable<GetMappedSpoke>> GetMappedSpokes(int coeId)
        {
            return await _repo.GetMappedSpokes(coeId);
        }

        public async Task<IEnumerable<SpokeMinimalDto>> GetSpokesForSpecialist(int specialistUserId)
        {
            var user = await _authRepo.GetById(specialistUserId);
            if (user == null || user.RoleId != UserRole.specialist || user.CoeId == null)
                return Enumerable.Empty<SpokeMinimalDto>();

            var spokes = await _repo.GetByCoeId(user.CoeId.Value);
            return spokes.Select(s => new SpokeMinimalDto
            {
                SpokeId = s.SpokeId,
                SpokeHospitalName = s.SpokeHospitalName
            });
        }

        public class GetMappedSpoke
        {
            public int SpokeId { get; set; }
            public string SpokeName { get; set; } = string.Empty;
        }

        public async Task<bool> UpdateBedsOnly(int spokeId, int icuBeds, int hduBeds, int otherBeds)
        {
            return await _repo.UpdateBedsOnly(spokeId, icuBeds, hduBeds, otherBeds);
        }

        public class SpokeMinimalDto
        {
            public int SpokeId { get; set; }
            public string SpokeHospitalName { get; set; } = string.Empty;
        }
    }
}