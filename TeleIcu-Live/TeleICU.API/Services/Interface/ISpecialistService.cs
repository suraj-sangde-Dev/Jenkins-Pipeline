using TeleICU.API.DTOs.SpecialistDtos;
using TeleICU.API.Models.AuthModels;
using static TeleICU.API.Services.SpecialistService;

namespace TeleICU.API.Services.Interface
{
    public interface ISpecialistService
    {
        Task<IEnumerable<SpecialistDto>> GetByCoeId(int coeId);
        Task<bool> AssignSpecialist(AssignSpecialistDto dto);
        Task<bool> RemoveSpecialist(int userId, int coeId);
        Task<IEnumerable<AllSpecialistDto>> GetAllSpecialists(int userId, string role);
        Task<SpecialistResponseDto> GetSpecialist(int id);
        Task<IEnumerable<SpecialistSimpleDto>> GetUnmappedSpecialistsByState(int stateId);
        Task<IEnumerable<SpecialistSimpleDto>> GetMappedSpecialistsByCoe(int coeId);
        Task<int> MapSpecialistToCoe(int specialistId, int coeId);
        Task<int> UnmapSpecialist(int specialistId);
        Task<IEnumerable<AvailableSpecialistDto>> GetAvailableSpecialists(int userId, string role, int? coeId = null);
    }
}