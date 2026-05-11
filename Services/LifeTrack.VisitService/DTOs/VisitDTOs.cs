namespace LifeTrack.VisitService.DTOs
{
    public class VisitDto
    {
        public long VisitID { get; set; }
        public long PatientID { get; set; }
        public long ProtocolID { get; set; }
        public DateTime VisitDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class CreateVisitRequest
    {
        public long PatientID { get; set; }
        public long ProtocolID { get; set; }
        public DateTime VisitDate { get; set; }
        public string Status { get; set; } = "Scheduled";
        public string Notes { get; set; } = string.Empty;
    }
}