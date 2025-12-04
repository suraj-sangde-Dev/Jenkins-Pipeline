using TeleICU.API.DTOs.NurseDtos;
using TeleICU.API.DTOs.SpecialistDtos;
using TeleICU.API.Models.NurseModels;
using static TeleICU.API.Services.NurseService;

namespace TeleICU.API.Repository.Interface
{
    public interface INurseRepository
    {
        Task<IEnumerable<NurseDto>> GetBySpokeId(int spokeId);
        Task<bool> AssignNurse(NurseModel nurse);
        Task<bool> RemoveNurse(int userId, int spokeId);
        Task<IEnumerable<NurseDto>> GetAllNursesAsync();
        Task<IEnumerable<NurseDto>> GetAllNursesByStateAsync(int stateId);
        Task<IEnumerable<NurseDto>> GetAllNursesBySpokeAsync(int spokeId);
        Task<NurseResponseDto> GetNurseById(int id);
        Task<IEnumerable<NurseDetailsDto>> GetNurseByCaseId(int caseId);
        Task<IEnumerable<NurseSimpleDto>> GetUnmappedNursesByState(int stateId);
        Task<IEnumerable<NurseSimpleDto>> GetMappedNursesBySpoke(int spokeId);
        Task<int> MapNurseToSpoke(int nurseId, int spokeId);
        Task<int> UnmapNurse(int nurseId);
    }
}