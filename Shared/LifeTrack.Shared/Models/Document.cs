// ============================================================
// Shared.CL / Models / Document.cs
// UPDATED: Added Title, FileURL, ReviewNotes, ReviewedBy,
// ReviewedAt and Reviewer navigation property to match the
// documents_alter.sql migration that added these columns.
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

        // Added by documents_alter.sql
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Type { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Version { get; set; } = string.Empty;

        // Added by documents_alter.sql
        [MaxLength(500)]
        public string FileURL { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        // Added by documents_alter.sql — nullable review fields
        [MaxLength(1000)]
        public string? ReviewNotes { get; set; }

        public long? ReviewedBy { get; set; }

        public DateTime? ReviewedAt { get; set; }

        // Navigation properties
        [ForeignKey("ProtocolID")]
        public Protocol? Protocol { get; set; }

        [ForeignKey("UploadedBy")]
        public User? Uploader { get; set; }

        // Added by documents_alter.sql FK_Documents_Users_ReviewedBy
        [ForeignKey("ReviewedBy")]
        public User? Reviewer { get; set; }
    }
}