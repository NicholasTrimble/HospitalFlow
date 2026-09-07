namespace HospitalFlow.Models
{
    public class Patient
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string MedicalRecordNumber { get; set; } = string.Empty;
        public DateTime AdmissionDate { get; set; } = DateTime.UtcNow;
        public PatientAcuity AcuityLevel { get; set; }
        public string? Notes { get; set; }

        public int? RoomId { get; set; }
        public Room? Room { get; set; }

    }

    public enum PatientAcuity
    {
        Stable,
        Monitoring,
        Critical
    }
}
