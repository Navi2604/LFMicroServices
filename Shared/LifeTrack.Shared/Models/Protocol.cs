// ============================================================
// Shared.CL / Models / Protocol.cs
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeTrack.Shared.Models
{
    public class Protocol
    {
        [Key]
        public long ProtocolID { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Phase { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        // ── NEW: store all phases as JSON ───────────────────
        public string? PhasesJson { get; set; }

        // ── Navigation ───────────────────────────────────────
        public ICollection<SiteProtocol> SiteProtocols { get; set; } = new List<SiteProtocol>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
        public ICollection<KPIReport> KPIReports { get; set; } = new List<KPIReport>();
    }
}