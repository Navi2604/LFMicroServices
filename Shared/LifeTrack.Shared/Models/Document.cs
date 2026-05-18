// ============================================================
// Shared.CL / Models / Document.cs
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeTrack.Shared.Models
{
    [Table("Documents")]
    public class Document
    {
        [Key]
        public long DocumentID { get; set; }

        public long ProtocolID { get; set; }
        public long UploadedBy { get; set; }

        [MaxLength(100)]
        public string Type { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Version { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        // Navigation
        [ForeignKey("ProtocolID")]
        public Protocol? Protocol { get; set; }

        [ForeignKey("UploadedBy")]
        public User? Uploader { get; set; }
    }
}