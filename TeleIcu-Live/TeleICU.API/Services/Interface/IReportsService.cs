using TeleICU.API.DTOs.ReportsDto;

namespace TeleICU.API.Services.Interface
{
    public interface IReportsService
    {
        Task<IEnumerable<ReportsDto>> GetConsultationsAsync(ConsultationRequest request);
    }
}
