// ============================================================
// SiteService.API / DTOs / SiteProtocolDTOs.cs
// FIXED:
// - InitiationDate is now nullable (DateTime?) — prevents 0001-01-01 default
// - SiteProtocolDto.InitiationDate is nullable too
// ============================================================

namespace LifeTrack.SiteService.DTOs
{
    public class SiteProtocolDto
    {
        public long SiteProtocolID { get; set; }
        public long SiteID { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public string SiteLocation { get; set; } = string.Empty;
        public long ProtocolID { get; set; }
        public string ProtocolTitle { get; set; } = string.Empty;
        public string ProtocolStatus { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public long InvestigatorID { get; set; }
        public string InvestigatorName { get; set; } = string.Empty;
        public string InvestigatorEmail { get; set; } = string.Empty;
        public string InvestigatorContact { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        // Nullable — null means not yet formally initiated
        public DateTime? InitiationDate { get; set; }
        // Existing fields stay untouched. ADD these two:
        
    }

    public class SiteProtocolFilterDto
    {
        public long? SiteID { get; set; }
        public long? ProtocolID { get; set; }
        public long? InvestigatorID { get; set; }
        public string? Status { get; set; }
    }

    public class CreateSiteProtocolRequest
    {
        public long SiteID { get; set; }
        public long ProtocolID { get; set; }
        public long InvestigatorID { get; set; }
        // Nullable — if not provided, defaults to today on the backend
        public DateTime? InitiationDate { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class UpdateSiteProtocolStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

}