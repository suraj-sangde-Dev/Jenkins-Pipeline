using TeleICU.API.DTOs.DoctorDtos;
using TeleICU.API.DTOs.NurseDtos;
using TeleICU.API.Models.DoctorModels;
using static TeleICU.API.Services.DoctorService;
using static TeleICU.API.Services.NurseService;

namespace TeleICU.API.Repository.Interface
{
    public interface IDoctorRepository
    {
        Task<IEnumerable<DoctorDto>> GetBySpokeId(int spokeId);
        Task<bool> AssignDoctor(DoctorModel doctor);
        Task<bool> RemoveDoctor(int userId, int spokeId);
        Task<IEnumerable<DoctorDto>> GetAllDoctors();
        Task<IEnumerable<DoctorDto>> GetAllDoctorsByState(int stateId);
        Task<IEnumerable<DoctorDto>> GetAllDoctorsBySpoke(int spokeId);
        Task<DoctorResponseDto> GetDoctorById(int id);
        Task<IEnumerable<DoctorSimpleDto>> GetUnmappedDoctorsByState(int stateId);
        Task<IEnumerable<DoctorSimpleDto>> GetMappedDoctorsBySpoke(int spokeId);
        Task<int> MapDoctorToSpoke(int doctorId, int spokeId);
        Task<int> UnmapDoctor(int doctorId);
    }
}