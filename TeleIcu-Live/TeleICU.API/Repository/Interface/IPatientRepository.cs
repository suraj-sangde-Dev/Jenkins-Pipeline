using TeleICU.API.DTOs.PatientDto;
using TeleICU.API.Models.AuthModels;
using TeleICU.API.Models.Beds;

namespace TeleICU.API.Repository.Interface
{
    public interface IPatientRepository
    {
        Task<int> RegisterPatient(RegisterPatientRequest request, int createdBy, int spokeId);
        Task<IEnumerable<BedModel>> GetVacantBeds(int spokeId);
        Task<bool> DischargePatient(int patientId);
        Task<IEnumerable<UserModel>> GetUsersByRoleAndSpoke(int roleId, int spokeId);
        Task<IEnumerable<object>> GetPatientsBySpoke(int spokeId);
        Task<IEnumerable<object>> GetPatientsByCoe(int coeId);
        Task<IEnumerable<PatientTileDto>> GetPatientTilesBySpoke(int spokeId);
        Task<IEnumerable<PatientTileDto>> GetPatientTilesByCoe(int coeId);
        Task<IEnumerable<VitalTrendDto>> GetVitalTrendsByPatient(int patientId, DateTime? fromDate);
        Task<string> GetPatientNameById(int patientId);
        Task<IEnumerable<PatientMedicationDto>> GetPatientMedications(int patientId);
        Task AddVitalForPatient(int patientId, TeleICU.API.DTOs.CaseDto.VitalDto vital);
        Task<PatientDetailDto?> GetPatientById(int patientId);
        Task<bool> UpdatePatient(EditPatientRequest request);
    }
}
