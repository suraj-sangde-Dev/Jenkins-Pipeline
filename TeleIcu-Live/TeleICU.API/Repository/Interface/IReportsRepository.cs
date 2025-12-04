using TeleICU.API.DTOs.ReportsDto;

namespace TeleICU.API.Repository.Interface
{
    public interface IReportsRepository
    {
        Task<IEnumerable<ReportsDto>> GetConsultationsAsync(ConsultationRequest request);
    }

}
