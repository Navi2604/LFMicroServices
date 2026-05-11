namespace LifeTrack.ProtocolService.DTOs
{
    public class ProtocolDto
    {
        public long ProtocolID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public long? InvestigatorID { get; set; }
        public string? PhasesJson { get; set; }
    }

    public class CreateProtocolRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public long? InvestigatorID { get; set; }
        public string? PhasesJson { get; set; }
    }
}