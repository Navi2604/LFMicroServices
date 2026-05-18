// ============================================================
// Shared.CL / Models / KPIReport.cs
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeTrack.Shared.Models
{
    [Table("KPIReports")]
    public class KPIReport
    {
        [Key]
        public long ReportID { get; set; }

        public long ProtocolID { get; set; }

        [MaxLength(200)]
        public string Scope { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,4)")]
        public decimal EnrollmentRate { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal DropoutRate { get; set; }

        public int AECount { get; set; }

        public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("ProtocolID")]
        public Protocol? Protocol { get; set; }
    }
}