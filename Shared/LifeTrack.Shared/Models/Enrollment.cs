// ============================================================
// Shared.CL / Models / Enrollment.cs
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeTrack.Shared.Models
{
    [Table("Enrollments")]
    public class Enrollment
    {
        [Key]
        public long EnrollmentID { get; set; }

        public long PatientID { get; set; }
        public long SiteProtocolID { get; set; }

        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
        public DateTime? ConsentDate { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Active";

        [MaxLength(300)]
        public string WithdrawalReason { get; set; } = string.Empty;

        // Navigation
        [ForeignKey("PatientID")]
        public Patient? Patient { get; set; }

        [ForeignKey("SiteProtocolID")]
        public SiteProtocol? SiteProtocol { get; set; }

        public ICollection<Visit> Visits { get; set; } = new List<Visit>();
    }
}