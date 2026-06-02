// ============================================================
// Shared.CL / Models / AdverseEvent.cs
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeTrack.Shared.Models
{
    [Table("AdverseEvents")]
    public class AdverseEvent
    {
        [Key]
        public long EventID { get; set; }

        public long PatientID { get; set; }
        public long ProtocolID { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Severity { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        public DateTime ReportedDate { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("PatientID")]
        public Patient? Patient { get; set; }
    }
}