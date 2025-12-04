using Dapper;
using System.Data;
using TeleICU.API.DTOs.ReportsDto;
using TeleICU.API.Repository.Interface;

namespace TeleICU.API.Repository
{
    public class ReportsRepository : IReportsRepository
    {
        private readonly DapperContext _context;

        public ReportsRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ReportsDto>> GetConsultationsAsync(ConsultationRequest request)
        {
            using var connection = _context.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@pPatientId", request.PatientId);
            parameters.Add("@pSpokeId", request.SpokeId);

            return await connection.QueryAsync<ReportsDto>(
                "GetConsultations",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }
    }
}
