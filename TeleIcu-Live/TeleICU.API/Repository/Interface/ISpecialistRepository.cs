using TeleICU.API.DTOs.SpecialistDtos;
using TeleICU.API.Models.AuthModels;
using TeleICU.API.Models.SpecialistModels;
using static TeleICU.API.Services.SpecialistService;

namespace TeleICU.API.Repository.Interface
{
    public interface ISpecialistRepository
    {
        Task<IEnumerable<SpecialistDto>> GetByCoeId(int coeId);
        Task<bool> AssignSpecialist(SpecialistModel specialist);
        Task<bool> RemoveSpecialist(int userId, int coeId);
        Task<IEnumerable<AllSpecialistDto>> GetAllSpecialists(int userId, string role);
        Task<SpecialistResponseDto> GetSpecialistById(int id);
        Task<IEnumerable<SpecialistSimpleDto>> GetUnmappedSpecialistsByState(int stateId);
        Task<IEnumerable<SpecialistSimpleDto>> GetMappedSpecialistsByCoe(int coeId);
        Task<int> MapSpecialistToCoe(int specialistId, int coeId);
        Task<int> UnmapSpecialist(int specialistId);
        Task<IEnumerable<AvailableSpecialistDto>> GetAllSpecialistsByState(int stateId);
        Task<IEnumerable<AvailableSpecialistDto>> GetAllSpecialistsByCoe(int coeId);
    }
}