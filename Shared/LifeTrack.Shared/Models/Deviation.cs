// ============================================================
// Shared.CL / Models / Deviation.cs
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeTrack.Shared.Models
{
    [Table("Deviations")]
    public class Deviation
    {
        [Key]
        public long DeviationID { get; set; }

        public long SiteProtocolID { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Severity { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        // Navigation
        [ForeignKey("SiteProtocolID")]
        public SiteProtocol? SiteProtocol { get; set; }
    }
}