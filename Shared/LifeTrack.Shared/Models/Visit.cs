// ============================================================
// Shared.CL / Models / Visit.cs
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeTrack.Shared.Models
{
    [Table("Visits")]
    public class Visit
    {
        [Key]
        public long VisitID { get; set; }

        public long EnrollmentID { get; set; }

        public DateTime VisitDate { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty;

        // Navigation
        [ForeignKey("EnrollmentID")]
        public Enrollment? Enrollment { get; set; }
    }
}