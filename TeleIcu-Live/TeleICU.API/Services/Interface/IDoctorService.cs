using TeleICU.API.DTOs.DoctorDtos;
using static TeleICU.API.Services.DoctorService;

namespace TeleICU.API.Services.Interface
{
    public interface IDoctorService
    {
        Task<IEnumerable<DoctorDto>> GetBySpokeId(int spokeId);
        Task<bool> AssignDoctor(AssignDoctorDto dto);
        Task<bool> RemoveDoctor(int userId, int spokeId);
        Task<IEnumerable<DoctorDto>> GetAllDoctors(int userId, string role);
        Task<DoctorResponseDto> GetDoctor(int id);
        Task<IEnumerable<DoctorSimpleDto>> GetUnmappedDoctorsByState(int userId);
        Task<IEnumerable<DoctorSimpleDto>> GetMappedDoctorsBySpoke(int spokeId);
        Task<int> MapDoctorToSpoke(int doctorId, int spokeId);
        Task<int> UnmapDoctor(int doctorId);
    }
}