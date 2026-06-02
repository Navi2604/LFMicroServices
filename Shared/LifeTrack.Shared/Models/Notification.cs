// ============================================================
// Shared.CL / Models / Notification.cs
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeTrack.Shared.Models
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        public long NotificationID { get; set; }

        public long UserID { get; set; }

        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Status { get; set; } = "Unread";

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("UserID")]
        public User? User { get; set; }
    }
}