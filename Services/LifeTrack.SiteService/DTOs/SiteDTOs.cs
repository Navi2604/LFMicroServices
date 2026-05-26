// ============================================================
// SiteService.API / DTOs / SiteDTOs.cs
// REPLACE YOUR EXISTING FILE WITH THIS
// WITH EMAIL AND CONTACT FIELDS
// ============================================================

namespace LifeTrack.SiteService.DTOs
{
    public class SiteDto
    {
        public long SiteID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Contact { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SiteFilterDto
    {
        public string? Name { get; set; }
        public string? Location { get; set; }
        public string? Status { get; set; }
    }

    public class CreateSiteRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Contact { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class UpdateSiteRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Contact { get; set; }
        public string Status { get; set; } = "Active";
    }
}