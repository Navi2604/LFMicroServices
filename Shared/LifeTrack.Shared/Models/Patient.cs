// ============================================================
// Shared.CL / Models / Patient.cs
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace LifeTrack.Shared.Models
{
    [Table("Patients")]
    public class Patient
    {
        [Key]
        public long PatientID { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public DateTime DOB { get; set; }

        [MaxLength(200)]
        public string ContactInfo { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        // Navigation
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<AdverseEvent> AdverseEvents { get; set; } = new List<AdverseEvent>();
    }
}