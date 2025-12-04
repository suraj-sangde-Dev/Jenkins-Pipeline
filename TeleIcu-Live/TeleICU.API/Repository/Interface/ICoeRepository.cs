using TeleICU.API.Models.CoeModels;

namespace TeleICU.API.Repository.Interface
{
    public interface ICoeRepository
    {
        Task<IEnumerable<CenterOfExcellenceModel>> GetAll();
        Task<CenterOfExcellenceModel?> GetById(int id);
        Task<bool> Create(CenterOfExcellenceModel coe);
        Task<bool> Update(CenterOfExcellenceModel coe);
        Task<bool> Delete(int id);
        Task<bool> AssignAdmin(int coeId, int adminId);
        Task<IEnumerable<CenterOfExcellenceModel>> GetByStateId(int stateId);
    }
}