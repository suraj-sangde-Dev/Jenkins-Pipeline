using TeleICU.API.DTOs.ReportsDto;
using TeleICU.API.Repository;
using TeleICU.API.Repository.Interface;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Services
{
    public class ReportsService : IReportsService
    {
        private readonly IReportsRepository _reportsRepository;

        public ReportsService(IReportsRepository reportsRepository)
        {
            _reportsRepository = reportsRepository;
        }

        public async Task<IEnumerable<ReportsDto>> GetConsultationsAsync(ConsultationRequest request)
        {
            return await _reportsRepository.GetConsultationsAsync(request);
        }
    }
}
