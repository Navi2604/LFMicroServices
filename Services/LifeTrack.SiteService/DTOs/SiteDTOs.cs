namespace LifeTrack.SiteService.DTOs
{
    public class SiteDto
    {
        public long SiteID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public long InvestigatorID { get; set; }
        public long? ProtocolID { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CreateSiteRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public long InvestigatorID { get; set; }
        public long? ProtocolID { get; set; }
        public string Status { get; set; } = "Active";
    }
}