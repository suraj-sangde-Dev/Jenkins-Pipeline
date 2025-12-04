using TeleICU.API.DTOs.SpokeDtos;
using TeleICU.API.Models.AuthModels;
using static TeleICU.API.Services.SpokeService;
using static TeleICU.API.Services.StateService;

namespace TeleICU.API.Services.Interface
{
    public interface ISpokeService
    {
        Task<IEnumerable<SpokeDto>> GetSpokesByRole(int userId, string role);
        Task<SpokeDto?> GetById(int id);
        Task<bool> Create(CreateSpokeDto dto, int createdBy, HttpRequest request);
        Task<bool> Update(int id, CreateSpokeDto dto, HttpRequest request);
        Task<bool> Delete(int id);
        Task<bool> AssignAdmin(int spokeId, int userId);
        Task<IEnumerable<UserModel>> GetAdminsBySpokeId(int spokeId);
        Task<IEnumerable<SpokeMinimalDto>> GetUnmappedSpokesByState(int userId);
        Task<bool> MapSpokeWithCoe(int spokeId, int coeId);
        Task<bool> UnmapSpokeFromCoe(int spokeId);
        Task<IEnumerable<GetMappedSpoke>> GetMappedSpokes(int coeId);
        Task<IEnumerable<SpokeMinimalDto>> GetSpokesForSpecialist(int specialistUserId);
        Task<bool> UpdateBedsOnly(int spokeId, int icuBeds, int hduBeds, int otherBeds);
    }
}