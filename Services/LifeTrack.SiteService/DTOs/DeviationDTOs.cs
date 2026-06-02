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
        public bool DmEdited { get; set; } = false;  // true when DM edited description/severity
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

    // DM edits description/severity on Reported deviations
    public class UpdateDeviationRequest
    {
        public string? Description { get; set; }
        public string? Severity { get; set; }
    }

    public class UpdateDeviationStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}