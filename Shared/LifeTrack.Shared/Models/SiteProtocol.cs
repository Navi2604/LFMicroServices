// ============================================================
// Shared.CL / Models / SiteProtocol.cs
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace LifeTrack.Shared.Models
{
    [Table("SiteProtocols")]
    public class SiteProtocol
    {
        [Key]
        public long SiteProtocolID { get; set; }

        public long SiteID { get; set; }
        public long ProtocolID { get; set; }
        public long InvestigatorID { get; set; }

        public DateTime InitiationDate { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        // Navigation
        [ForeignKey("SiteID")]
        public Site? Site { get; set; }

        [ForeignKey("ProtocolID")]
        public Protocol? Protocol { get; set; }

        [ForeignKey("InvestigatorID")]
        public User? Investigator { get; set; }

        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<Deviation> Deviations { get; set; } = new List<Deviation>();
    }
}