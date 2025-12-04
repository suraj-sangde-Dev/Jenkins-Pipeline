using TeleICU.API.DTOs.NurseDtos;
using TeleICU.API.DTOs.SpecialistDtos;
using static TeleICU.API.Services.NurseService;

namespace TeleICU.API.Services.Interface
{
    public interface INurseService
    {
        Task<IEnumerable<NurseDto>> GetBySpokeId(int spokeId);
        Task<bool> AssignNurse(AssignNurseDto dto);
        Task<bool> RemoveNurse(int userId, int spokeId);
        Task<IEnumerable<NurseDto>> GetAllNursesAsync(int userId, string role);
        Task<NurseResponseDto> GetNurse(int id);
        Task<IEnumerable<NurseSimpleDto>> GetUnmappedNursesByState(int userId);
        Task<IEnumerable<NurseSimpleDto>> GetMappedNursesBySpoke(int spokeId);
        Task<int> MapNurseToSpoke(int nurseId, int spokeId);
        Task<int> UnmapNurse(int nurseId);
        Task<IEnumerable<NurseDetailsDto>> GetNurseByCaseId(int caseId);
    }
}