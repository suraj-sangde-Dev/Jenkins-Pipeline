using TeleICU.API.DTOs.CaseDto;

namespace TeleICU.API.DTOs.PrescriptionDtos
{
    public class SyncPrescriptionDto
    {
        public int CaseId { get; set; }
        public string? EncounterId { get; set; }
        public string? Examination { get; set; }
        public string? Advice { get; set; }
        public List<PreAdmitMedicationDto> Medications { get; set; } = new();
    }
}
