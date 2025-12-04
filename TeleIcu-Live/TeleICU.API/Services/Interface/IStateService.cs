using TeleICU.API.DTOs.SpokeDtos;
using TeleICU.API.DTOs.StateDtos;
using TeleICU.API.Models.AuthModels;
using static TeleICU.API.Services.StateService;

namespace TeleICU.API.Services.Interface
{
    public interface IStateService
    {
        Task<IEnumerable<StateDto>> GetAllStates();
        Task<IEnumerable<DistrictDto>> GetDistrictsAsync(int stateCode);
        Task<IEnumerable<CityDto>> GetCitiesAsync(int districtCode);
        Task<StateDto?> GetById(int id);
        //Task<bool> AssignAdmin(int stateId, int userId);
        //Task<IEnumerable<StateNetworkDetailsDto>> GetStateNetworkById(int stateId);
        Task<StateAdminResponseDto?> GetAdminByStateId(int stateId);
        Task<IEnumerable<StateDto>> GetAssignedStates();
        Task<IEnumerable<StateDto>> GetUnassignedStates();
        Task<bool> UnassignStateAdmin(int stateId);
    }
}