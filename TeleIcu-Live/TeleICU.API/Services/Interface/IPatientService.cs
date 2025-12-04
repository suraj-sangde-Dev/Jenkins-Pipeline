using TeleICU.API.DTOs.PatientDto;
using TeleICU.API.Models.AuthModels;
using TeleICU.API.Models.Beds;

namespace TeleICU.API.Services.Interface
{
    public interface IPatientService
    {
        Task<bool> RegisterPatient(RegisterPatientRequest request, int createdBy);
        Task<IEnumerable<BedModel>> GetVacantBeds(int spokeId);
        Task<IEnumerable<UserModel>> GetDoctorsForPatient(int userId);
        Task<bool> DischargePatient(int patientId);
        Task<IEnumerable<object>> GetPatientsForUser(int userId);
        Task<IEnumerable<PatientTileDto>> GetPatientTilesForUser(int userId);
        Task<VitalTrendResponse> GetVitalTrends(int userId, VitalTrendRequest request);
        Task<IEnumerable<PatientMedicationDto>> GetPatientMedications(int userId, PatientMedicationQuery query);
        Task RecordVital(int userId, int patientId, TeleICU.API.DTOs.CaseDto.VitalDto request);
        Task<PatientDetailDto?> GetPatientById(int userId, int patientId);
        Task<bool> UpdatePatient(int userId, EditPatientRequest request);
    }
}
