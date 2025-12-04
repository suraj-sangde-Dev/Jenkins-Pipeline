using TeleICU.API.DTOs.CoeDtos;

namespace TeleICU.API.Services.Interface
{
    public interface ICoeService
    {
        Task<IEnumerable<CoeDto>> GetAll(int userId, string role);
        Task<CoeDto?> GetById(int id);
        Task<bool> Create(CreateCoeDto dto, int createdBy, int stateId, HttpRequest request);
        Task<bool> Update(int id, CreateCoeDto dto, int stateId, HttpRequest request);
        Task<bool> Delete(int id);
        Task<bool> AssignAdmin(int coeId, int adminId);
        Task<IEnumerable<CoeDto>> GetByStateId(int stateId);
    }
}