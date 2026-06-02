// ============================================================
// Shared.CL / Models / AuditLog.cs
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeTrack.Shared.Models
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        public long AuditID { get; set; }

        public long UserID { get; set; }

        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;

        public long? EntityID { get; set; }

        [MaxLength(1000)]
        public string Details { get; set; } = string.Empty;

        public DateTime ActionTime { get; set; } = DateTime.UtcNow;
    }
}