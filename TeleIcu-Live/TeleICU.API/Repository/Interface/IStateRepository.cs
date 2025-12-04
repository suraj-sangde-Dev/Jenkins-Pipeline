using TeleICU.API.DTOs.SpokeDtos;
using TeleICU.API.DTOs.StateDtos;
using TeleICU.API.Models.SpokeModels;
using TeleICU.API.Models.StateModels;

namespace TeleICU.API.Repository.Interface
{
    public interface IStateRepository
    {
        Task<IEnumerable<StateModel>> GetAll();
        Task<IEnumerable<DistrictDto>> GetDistrictsByStateCodeAsync(int stateCode);
        Task<IEnumerable<CityDto>> GetCitiesByDistrictCodeAsync(int districtCode);
        Task<StateModel?> GetById(int id);
        //Task<bool> AssignAdmin(int stateId, int userId);
        Task<StateModel?> GetByUserId(int userId);
        Task<bool> UnassignStateAdmin(int stateId);
        //Task<IEnumerable<StateNetworkDetailsDto>> GetStateNetworkById(int stateId);
        Task<StateAdminResponseDto?> GetAdminByStateId(int stateId);
        Task<IEnumerable<StateModel>> GetAssignedStates();
        Task<IEnumerable<StateModel>> GetUnassignedStates();
    }
}