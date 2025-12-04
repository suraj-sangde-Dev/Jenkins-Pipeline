using Dapper;
using TeleICU.API.DTOs.DashBoard;
using TeleICU.API.Repository.Interface;

namespace TeleICU.API.Repository
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly DapperContext _context;

        public DashboardRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<DashboardCountsDto> GetDashboardCounts(int userId, string role)
        {
            DashboardCountsDto result = new();
            using var conn = _context.CreateConnection();

            if (role == "super_admin")
            {
                // Network
                result.States = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM StateAdminsMapping");
                result.CoE = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM CentersOfExcellence");
                result.Spokes = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Spokes");

                // Beds
                result.TotalBeds = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Beds");
                result.OccupiedICU = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Beds WHERE BedType='ICU' AND Status='Occupied'");
                result.OccupiedHDU = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Beds WHERE BedType='HDU' AND Status='Occupied'");
                result.OccupiedOther = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Beds WHERE BedType IN ('General','Other') AND Status='Occupied'");
                result.VacantICU = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Beds WHERE BedType='ICU' AND Status='Vacant'");
                result.VacantHDU = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Beds WHERE BedType='HDU' AND Status='Vacant'");
                result.VacantOther = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Beds WHERE BedType IN ('General','Other') AND Status='Vacant'");

                // Patients
                result.TotalPatients = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Patients");
                result.DischargedPatients = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Patients WHERE IsActive = 0");
                result.AdmittedPatients = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Patients WHERE IsActive = 1");

                // Users
                result.Doctor = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users WHERE RoleId = 6");
                result.Nurse = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users WHERE RoleId = 7");
                result.Specialist = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users WHERE RoleId = 5");
                result.Admin = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users WHERE RoleId IN (1,2,3,4)");
                result.Total = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users");
            }
            else if (role == "state_admin")
            {
                var stateId = await conn.ExecuteScalarAsync<int?>(
                    @"SELECT StateId FROM StateAdminsMapping WHERE UserId = @UserId",
                    new { UserId = userId });

                if (stateId == null)
                    throw new Exception("State not assigned to this state admin.");

                result.States = 1;

                result.CoE = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM CentersOfExcellence WHERE StateId = @StateId",
                    new { StateId = stateId });

                result.Spokes = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Spokes WHERE StateId = @StateId",
                    new { StateId = stateId });

                // Beds
                result.TotalBeds = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Beds b
          INNER JOIN Spokes s ON b.SpokeId = s.SpokeId
          WHERE s.StateId = @StateId", new { StateId = stateId });

                result.OccupiedICU = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Beds b
          INNER JOIN Spokes s ON b.SpokeId = s.SpokeId
          WHERE s.StateId = @StateId AND BedType='ICU' AND Status='Occupied'", new { StateId = stateId });

                result.VacantICU = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Beds b
          INNER JOIN Spokes s ON b.SpokeId = s.SpokeId
          WHERE s.StateId = @StateId AND BedType='ICU' AND Status='Vacant'", new { StateId = stateId });

                result.OccupiedHDU = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Beds b
          INNER JOIN Spokes s ON b.SpokeId = s.SpokeId
          WHERE s.StateId = @StateId AND BedType='HDU' AND Status='Occupied'", new { StateId = stateId });

                result.VacantHDU = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Beds b
          INNER JOIN Spokes s ON b.SpokeId = s.SpokeId
          WHERE s.StateId = @StateId AND BedType='HDU' AND Status='Vacant'", new { StateId = stateId });

                result.OccupiedOther = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Beds b
          INNER JOIN Spokes s ON b.SpokeId = s.SpokeId
          WHERE s.StateId = @StateId AND BedType IN ('General','Other') AND Status='Occupied'", new { StateId = stateId });

                result.VacantOther = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Beds b
          INNER JOIN Spokes s ON b.SpokeId = s.SpokeId
          WHERE s.StateId = @StateId AND BedType IN ('General','Other') AND Status='Vacant'", new { StateId = stateId });

                // Patients
                result.TotalPatients = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Patients p
          INNER JOIN Spokes s ON p.SpokeId = s.SpokeId
          WHERE s.StateId = @StateId", new { StateId = stateId });

                result.AdmittedPatients = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Patients p
          INNER JOIN Spokes s ON p.SpokeId = s.SpokeId
          WHERE s.StateId = @StateId AND p.IsActive = 1", new { StateId = stateId });

                result.DischargedPatients = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Patients p
          INNER JOIN Spokes s ON p.SpokeId = s.SpokeId
          WHERE s.StateId = @StateId AND p.IsActive = 0", new { StateId = stateId });

                // Users
                result.Doctor = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Users WHERE RoleId = 6 AND StateId = @StateId",
                    new { StateId = stateId });

                result.Nurse = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Users WHERE RoleId = 7 AND StateId = @StateId",
                    new { StateId = stateId });

                result.Specialist = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Users u
          INNER JOIN SpecialistsMapping sm ON u.UserId = sm.UserId
          INNER JOIN CentersOfExcellence c ON sm.CoeId = c.CoeId
          WHERE u.RoleId = 5 AND c.StateId = @StateId", new { StateId = stateId });

                result.Admin = 1;
                result.Total = result.Doctor + result.Nurse + result.Specialist + result.Admin;
            }
            else if (role == "coe_admin")
            {
                var coeId = await conn.ExecuteScalarAsync<int?>(
                    @"SELECT CoeId FROM CoeAdminsMapping WHERE UserId = @UserId",
                    new { UserId = userId });

                if (coeId == null)
                    throw new Exception("COE not assigned to this CoE admin.");

                result.CoE = 1;

                result.Spokes = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Spokes WHERE CoeId = @CoeId",
                    new { CoeId = coeId });

                // Beds
                result.TotalBeds = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Beds b
          INNER JOIN Spokes s ON b.SpokeId = s.SpokeId
          WHERE s.CoeId = @CoeId", new { CoeId = coeId });

                result.OccupiedICU = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Beds b
          INNER JOIN Spokes s ON b.SpokeId = s.SpokeId
          WHERE s.CoeId = @CoeId AND BedType='ICU' AND Status='Occupied'", new { CoeId = coeId });

                result.VacantICU = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Beds b
          INNER JOIN Spokes s ON b.SpokeId = s.SpokeId
          WHERE s.CoeId = @CoeId AND BedType='ICU' AND Status='Vacant'", new { CoeId = coeId });

                result.OccupiedHDU = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Beds b
          INNER JOIN Spokes s ON b.SpokeId = s.SpokeId
          WHERE s.CoeId = @CoeId AND BedType='HDU' AND Status='Occupied'", new { CoeId = coeId });

                result.VacantHDU = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Beds b
          INNER JOIN Spokes s ON b.SpokeId = s.SpokeId
          WHERE s.CoeId = @CoeId AND BedType='HDU' AND Status='Vacant'", new { CoeId = coeId });

                result.OccupiedOther = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Beds b
          INNER JOIN Spokes s ON b.SpokeId = s.SpokeId
          WHERE s.CoeId = @CoeId AND BedType IN ('General','Other') AND Status='Occupied'", new { CoeId = coeId });

                result.VacantOther = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Beds b
          INNER JOIN Spokes s ON b.SpokeId = s.SpokeId
          WHERE s.CoeId = @CoeId AND BedType IN ('General','Other') AND Status='Vacant'", new { CoeId = coeId });

                // Patients
                result.TotalPatients = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Patients p
          INNER JOIN Spokes s ON p.SpokeId = s.SpokeId
          WHERE s.CoeId = @CoeId", new { CoeId = coeId });

                result.AdmittedPatients = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Patients p
          INNER JOIN Spokes s ON p.SpokeId = s.SpokeId
          WHERE s.CoeId = @CoeId AND p.IsActive = 1", new { CoeId = coeId });

                result.DischargedPatients = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Patients p
          INNER JOIN Spokes s ON p.SpokeId = s.SpokeId
          WHERE s.CoeId = @CoeId AND p.IsActive = 0", new { CoeId = coeId });

                // Users
                result.Doctor = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Users WHERE RoleId = 6 AND CoeId = @CoeId",
                    new { CoeId = coeId });

                result.Nurse = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Users WHERE RoleId = 7 AND CoeId = @CoeId",
                    new { CoeId = coeId });

                result.Specialist = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Users u
          INNER JOIN SpecialistsMapping sm ON u.UserId = sm.UserId
          WHERE u.RoleId = 5 AND sm.CoeId = @CoeId", new { CoeId = coeId });

                result.Admin = 1;
                result.Total = result.Doctor + result.Nurse + result.Specialist + result.Admin;
            }
            else if (role == "spoke_admin")
            {
                var spokeId = await conn.ExecuteScalarAsync<int?>(
                    @"SELECT SpokeId FROM SpokeAdminsMapping WHERE UserId = @UserId",
                    new { UserId = userId });

                if (spokeId == null)
                    throw new Exception("Spoke not assigned to this Spoke admin.");

                result.Spokes = 1;

                // Beds
                result.TotalBeds = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Beds WHERE SpokeId = @SpokeId", new { SpokeId = spokeId });

                result.OccupiedICU = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Beds WHERE SpokeId = @SpokeId AND BedType='ICU' AND Status='Occupied'", new { SpokeId = spokeId });

                result.VacantICU = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Beds WHERE SpokeId = @SpokeId AND BedType='ICU' AND Status='Vacant'", new { SpokeId = spokeId });

                result.OccupiedHDU = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Beds WHERE SpokeId = @SpokeId AND BedType='HDU' AND Status='Occupied'", new { SpokeId = spokeId });

                result.VacantHDU = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Beds WHERE SpokeId = @SpokeId AND BedType='HDU' AND Status='Vacant'", new { SpokeId = spokeId });

                result.OccupiedOther = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Beds WHERE SpokeId = @SpokeId AND BedType IN ('General','Other') AND Status='Occupied'", new { SpokeId = spokeId });

                result.VacantOther = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Beds WHERE SpokeId = @SpokeId AND BedType IN ('General','Other') AND Status='Vacant'", new { SpokeId = spokeId });

                // Patients
                result.TotalPatients = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Patients WHERE SpokeId = @SpokeId", new { SpokeId = spokeId });

                result.AdmittedPatients = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Patients WHERE SpokeId = @SpokeId AND IsActive = 1", new { SpokeId = spokeId });

                result.DischargedPatients = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Patients WHERE SpokeId = @SpokeId AND IsActive = 0", new { SpokeId = spokeId });

                // Users
                result.Doctor = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Users WHERE RoleId = 6 AND SpokeId = @SpokeId",
                    new { SpokeId = spokeId });

                result.Nurse = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Users WHERE RoleId = 7 AND SpokeId = @SpokeId",
                    new { SpokeId = spokeId });

                result.Admin = 1;
                result.Total = result.Doctor + result.Nurse + result.Admin;
            }

            return result;
        }


    }
}
