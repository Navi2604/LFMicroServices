// ============================================================
// SiteService.API / DTOs / DeviationDTOs.cs
// CREATE THIS NEW FILE
// ============================================================

namespace LifeTrack.SiteService.DTOs
{
    public class DeviationDto
    {
        public long DeviationID { get; set; }
        public long SiteProtocolID { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public string ProtocolTitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class DeviationFilterDto
    {
        public long? SiteProtocolID { get; set; }
        public string? Severity { get; set; }
        public string? Status { get; set; }
    }

    public class CreateDeviationRequest
    {
        public long SiteProtocolID { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
    }

    public class UpdateDeviationStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}