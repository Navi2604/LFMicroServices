namespace LifeTrack.PatientService.DTOs
{
    public class PatientDto
    {
        public long PatientID { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime DOB { get; set; }
        public string ContactInfo { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string EnrollmentStatus { get; set; } = string.Empty;
        public long? EnrolledBy { get; set; }
    }

    public class EnrollPatientRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime DOB { get; set; }
        public string ContactInfo { get; set; } = string.Empty;
        public long? ProtocolID { get; set; }
    }

    public class UpdateStatusRequest
    {
        public string EnrollmentStatus { get; set; } = string.Empty;
    }
}