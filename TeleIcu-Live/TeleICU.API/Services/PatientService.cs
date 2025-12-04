using TeleICU.API.DTOs.PatientDto;
using TeleICU.API.Models.AuthModels;
using TeleICU.API.Models.Beds;
using TeleICU.API.Repository;
using TeleICU.API.Repository.Interface;
using TeleICU.API.Services.Interface;

namespace TeleICU.API.Services
{
    public class PatientService : IPatientService
    {
        private readonly IPatientRepository _repository;
        private readonly IAuthRepository _userRepository;
        private readonly ISpokeRepository _spokeRepository;

        public PatientService(IPatientRepository repository, IAuthRepository userRepository, ISpokeRepository spokeRepository)
        {
            _repository = repository;
            _userRepository = userRepository;
            _spokeRepository = spokeRepository;
        }

        public async Task<bool> RegisterPatient(RegisterPatientRequest request, int createdBy)
        {
            var user = await _userRepository.GetById(createdBy);
            if (user == null || !user.SpokeId.HasValue)
                throw new Exception("User is not assigned to any spoke hospital.");

            int spokeId = user.SpokeId.Value;

            // 🔹 Fetch doctor details
            var doctor = await _userRepository.GetById(request.DoctorId);
            if (doctor == null || doctor.RoleId != UserRole.doctor || doctor.SpokeId != spokeId)
                throw new Exception("Invalid doctor. You can only assign doctors from your spoke hospital.");

            await _repository.RegisterPatient(request, createdBy, spokeId);

            return true;
        }

        public async Task<IEnumerable<UserModel>> GetDoctorsForPatient(int userId)
        {
            var user = await _userRepository.GetById(userId);
            if (user?.SpokeId == null)
                return Enumerable.Empty<UserModel>();

            return await _repository.GetUsersByRoleAndSpoke((int)UserRole.doctor, user.SpokeId.Value);
        }

        public async Task<IEnumerable<BedModel>> GetVacantBeds(int createdBy)
        {
            // get user (doctor/nurse)
            var user = await _userRepository.GetById(createdBy);
            if (user == null || user.SpokeId == null)
                throw new Exception("User is not assigned to any spoke hospital.");

            return await _repository.GetVacantBeds(user.SpokeId.Value);
        }

        public async Task<bool> DischargePatient(int patientId)
        {
            return await _repository.DischargePatient(patientId);
        }

        public async Task<IEnumerable<object>> GetPatientsForUser(int userId)
        {
            var user = await _userRepository.GetById(userId);
            //if (user?.SpokeId == null)
            //    throw new Exception("User is not assigned to any spoke hospital {userId}");

            //return await _repository.GetPatientsBySpoke(user.SpokeId.Value);
            if (user == null)
                throw new Exception("User not found.");

            // Nurse / Doctor -> All can see only their own Spoke
            if (user.RoleId == UserRole.nurse || user.RoleId == UserRole.doctor)
            {
                if (user.SpokeId == null)
                    throw new Exception("User is not assigned to any spoke hospital.");

                return await _repository.GetPatientsBySpoke(user.SpokeId.Value);
            }

            // Specialist → Should see all spokes under HIS COE
            if (user.RoleId == UserRole.specialist)
            {
                if (user.CoeId == null)
                    throw new Exception("Specialist is not assigned to any COE.");

                return await _repository.GetPatientsByCoe(user.CoeId.Value);
            }

            // Other roles (future)
            return Enumerable.Empty<PatientTileDto>();
        }

        public async Task<IEnumerable<PatientTileDto>> GetPatientTilesForUser(int userId)
        {
            var user = await _userRepository.GetById(userId);
            if (user == null)
                throw new Exception("User not found.");

            // Nurse / Doctor -> All can see only their own Spoke
            if (user.RoleId == UserRole.nurse
                || user.RoleId == UserRole.doctor)
            {
                if (user.SpokeId == null)
                    throw new Exception("User is not assigned to any spoke hospital.");

                return await _repository.GetPatientTilesBySpoke(user.SpokeId.Value);
            }

            // Specialist → Should see all spokes under HIS COE
            if (user.RoleId == UserRole.specialist)
            {
                if (user.CoeId == null)
                    throw new Exception("Specialist is not assigned to any COE.");

                return await _repository.GetPatientTilesByCoe(user.CoeId.Value);
            }

            // Other roles (future)
            return Enumerable.Empty<PatientTileDto>();
        }

        public async Task<VitalTrendResponse> GetVitalTrends(int userId, VitalTrendRequest request)
        {
            var user = await _userRepository.GetById(userId);
            if (user == null)
                throw new Exception("User not found.");

            // Access control
            bool hasAccess = false;

            if (user.RoleId == UserRole.doctor || user.RoleId == UserRole.nurse)
            {
                if (user.SpokeId == null)
                    throw new Exception("User is not assigned to any spoke hospital.");

                // Verify that the patient belongs to the user's spoke
                var patientTiles = await _repository.GetPatientTilesBySpoke(user.SpokeId.Value);
                hasAccess = patientTiles.Any(p => p.PatientId == request.PatientId);
            }
            else if (user.RoleId == UserRole.specialist)
            {
                if (user.CoeId == null)
                    throw new Exception("Specialist is not assigned to any COE.");

                // Fetch patient's spoke and validate it belongs to specialist's COE
                var patient = await _repository.GetPatientById(request.PatientId);
                if (patient != null)
                {
                    var spokes = await _spokeRepository.GetByCoeId(user.CoeId.Value);
                    hasAccess = spokes.Any(s => s.SpokeId == patient.SpokeId);
                }
            }
            else
            {
                // Other roles not allowed
                hasAccess = false;
            }

            if (!hasAccess)
                throw new Exception("Patient not found or you don't have access to this patient's data.");

            // Compute fromDate based on TimeRange (supports values like "2", "2h", "2 hour", "24 hours", "live", "from admission")
            DateTime? fromDate = null;
            var raw = (request.TimeRange ?? string.Empty).Trim();
            var normalized = raw.ToLowerInvariant();

            if (normalized.Contains("live"))
            {
                fromDate = DateTime.UtcNow.AddHours(-2);
            }
            else if (normalized.Contains("from") && normalized.Contains("admission"))
            {
                fromDate = null; // all available data
            }
            else
            {
                // Extract digits anywhere in the string
                var digitsOnly = new string(normalized.Where(char.IsDigit).ToArray());
                if (!string.IsNullOrEmpty(digitsOnly) && int.TryParse(digitsOnly, out int hours) && hours > 0)
                {
                    fromDate = DateTime.UtcNow.AddHours(-hours);
                }
            }

            var vitals = await _repository.GetVitalTrendsByPatient(request.PatientId, fromDate);
            var patientName = await _repository.GetPatientNameById(request.PatientId);

            return new VitalTrendResponse
            {
                PatientId = request.PatientId,
                PatientName = patientName,
                TimeRange = request.TimeRange,
                Vitals = vitals.ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<IEnumerable<PatientMedicationDto>> GetPatientMedications(int userId, PatientMedicationQuery query)
        {
            var user = await _userRepository.GetById(userId);
            if (user?.SpokeId == null)
                throw new Exception("User is not assigned to any spoke hospital.");

            // Ensure the patient belongs to the user's spoke
            var patients = await _repository.GetPatientsBySpoke(user.SpokeId.Value);
            var patientAllowed = patients.Any(p => ((dynamic)p).PatientId == query.PatientId);

            if (!patientAllowed)
                throw new Exception("Patient not found or you don't have access to this patient's data.");

            return await _repository.GetPatientMedications(query.PatientId);

        }

        public async Task RecordVital(int userId, int patientId, TeleICU.API.DTOs.CaseDto.VitalDto request)
        {
            var user = await _userRepository.GetById(userId);
            if (user?.SpokeId == null)
                throw new Exception("User is not assigned to any spoke hospital.");

            // Ensure the patient belongs to the user's spoke
            var patients = await _repository.GetPatientsBySpoke(user.SpokeId.Value);
            var patientAllowed = patients.Any(p => ((dynamic)p).PatientId == patientId);
            if (!patientAllowed)
                throw new Exception("Patient not found or you don't have access to this patient's data.");

            await _repository.AddVitalForPatient(patientId, request);
        }

        public async Task<PatientDetailDto?> GetPatientById(int userId, int patientId)
        {
            var user = await _userRepository.GetById(userId);
            if (user?.SpokeId == null)
                throw new Exception("User is not assigned to any spoke hospital.");

            var patient = await _repository.GetPatientById(patientId);
            
            // Verify patient belongs to user's spoke
            if (patient == null || patient.SpokeId != user.SpokeId)
                return null;

            return patient;
        }

        public async Task<bool> UpdatePatient(int userId, EditPatientRequest request)
        {
            var user = await _userRepository.GetById(userId);
            if (user?.SpokeId == null)
                throw new Exception("User is not assigned to any spoke hospital.");

            // Get patient to verify access and spoke
            var patient = await _repository.GetPatientById(request.PatientId);
            if (patient == null || patient.SpokeId != user.SpokeId)
                throw new Exception("Patient not found or you don't have access to this patient.");

            // Verify doctor belongs to same spoke
            var doctor = await _userRepository.GetById(request.DoctorId);
            if (doctor == null || doctor.RoleId != UserRole.doctor || doctor.SpokeId != user.SpokeId)
                throw new Exception("Invalid doctor. You can only assign doctors from your spoke hospital.");

            return await _repository.UpdatePatient(request);
        }
    }
}
