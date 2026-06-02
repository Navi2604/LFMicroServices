// ============================================================
// Shared.CL / Models / User.cs
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeTrack.Shared.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public long UserID { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int RoleID { get; set; }

        [Required, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // Navigation
        [ForeignKey("RoleID")]
        public Role? Role { get; set; }

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}