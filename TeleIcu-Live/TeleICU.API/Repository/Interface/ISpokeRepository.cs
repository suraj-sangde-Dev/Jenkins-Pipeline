using TeleICU.API.DTOs.SpokeDtos;
using TeleICU.API.Models.AuthModels;
using TeleICU.API.Models.SpokeModels;
using static TeleICU.API.Services.SpokeService;

namespace TeleICU.API.Repository.Interface
{
    public interface ISpokeRepository
    {
        Task<IEnumerable<SpokeModel>> GetAll();
        Task<SpokeModel?> GetById(int id);
        Task<IEnumerable<SpokeModel>> GetByCoeId(int coeId);
        Task<IEnumerable<SpokeModel>> GetByStateId(int stateId);
        Task<IEnumerable<SpokeModel>> GetBySpokeId(int spokeId);
        Task<bool> Create(SpokeModel spoke);
        Task<bool> Update(SpokeModel spoke);
        Task<bool> Delete(int id);
        Task<bool> AssignAdmin(int spokeId, int userId);
        Task<IEnumerable<UserModel>> GetAdminsBySpokeId(int spokeId);
        Task<IEnumerable<SpokeModel>> GetByStateAdminUserId(int userId);
        Task<IEnumerable<SpokeModel>> GetUnmappedSpokesByStateId(int stateId);
        Task<bool> MapSpokeWithCoe(int spokeId, int coeId);
        Task<bool> UnmapSpokeFromCoe(int spokeId);
        Task<IEnumerable<GetMappedSpoke>> GetMappedSpokes(int coeId);
        Task<bool> UpdateBedsOnly(int spokeId, int icuBeds, int hduBeds, int otherBeds);
    }
}