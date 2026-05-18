// ============================================================
// SiteService.API / DTOs / SiteDTOs.cs
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace LifeTrack.SiteService.DTOs
{
    // ── Site ─────────────────────────────────────────────────

    public class SiteDto
    {
        public long SiteID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class CreateSiteRequest
    {
        [Required(ErrorMessage = "Site name is required.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        public string Status { get; set; } = "Active";
    }

    public class SiteFilterDto
    {
        public string? Name { get; set; }
        public string? Location { get; set; }
        public string? Status { get; set; }
    }

    // ── SiteProtocol ─────────────────────────────────────────

    public class SiteProtocolDto
    {
        public long SiteProtocolID { get; set; }
        public long SiteID { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public long ProtocolID { get; set; }
        public string ProtocolTitle { get; set; } = string.Empty;
        public long InvestigatorID { get; set; }
        public string InvestigatorName { get; set; } = string.Empty;
        public DateTime InitiationDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateSiteProtocolRequest
    {
        [Required(ErrorMessage = "Site is required.")]
        public long SiteID { get; set; }

        [Required(ErrorMessage = "Protocol is required.")]
        public long ProtocolID { get; set; }

        [Required(ErrorMessage = "Investigator is required.")]
        public long InvestigatorID { get; set; }

        public DateTime InitiationDate { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Active";
    }

    public class UpdateSiteProtocolStatusRequest
    {
        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = string.Empty;
    }

    public class SiteProtocolFilterDto
    {
        public long? SiteID { get; set; }
        public long? ProtocolID { get; set; }
        public string? Status { get; set; }
    }

    // ── Deviation ────────────────────────────────────────────

    public class DeviationDto
    {
        public long DeviationID { get; set; }
        public long SiteProtocolID { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class CreateDeviationRequest
    {
        [Required(ErrorMessage = "SiteProtocol is required.")]
        public long SiteProtocolID { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Severity is required.")]
        public string Severity { get; set; } = string.Empty;
    }

    public class UpdateDeviationStatusRequest
    {
        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = string.Empty;
    }

    public class DeviationFilterDto
    {
        public long? SiteProtocolID { get; set; }
        public string? Severity { get; set; }
        public string? Status { get; set; }
    }
}