// ============================================================
// Shared.CL / Models / Site.cs
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace LifeTrack.Shared.Models
{
    [Table("Sites")]
    public class Site
    {
        [Key]
        public long SiteID { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        // Navigation
        public ICollection<SiteProtocol> SiteProtocols { get; set; } = new List<SiteProtocol>();
    }
}