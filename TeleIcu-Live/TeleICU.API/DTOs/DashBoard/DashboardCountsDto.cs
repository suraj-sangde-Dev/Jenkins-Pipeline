namespace TeleICU.API.DTOs.DashBoard
{
    public class DashboardCountsDto
    {
        //Network
        public int States { get; set; }
        public int CoE { get; set; }
        public int Spokes { get; set; }

        // Beds
        public int TotalBeds { get; set; }
        public int OccupiedICU { get; set; }
        public int VacantICU { get; set; }
        public int OccupiedHDU { get; set; }
        public int VacantHDU { get; set; }
        public int OccupiedOther { get; set; }
        public int VacantOther { get; set; }

        // Patients
        public int TotalPatients { get; set; }
        public int DischargedPatients { get; set; }
        public int AdmittedPatients { get; set; }
        public int CriticalPatients { get; set; }
        public int SeverePatients { get; set; }
        public int NormalPatients { get; set; }

        //Users
        public int Total { get; set; }
        public int Admin{ get; set; }
        public int Doctor { get; set; }
        public int Nurse { get; set; }
        public int Specialist { get; set; }
    }
}
