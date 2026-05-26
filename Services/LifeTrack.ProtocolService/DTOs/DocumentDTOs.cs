// ============================================================
// ProtocolService.API / DTOs / DocumentDTOs.cs
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace LifeTrack.ProtocolService.DTOs
{
    // ── Response DTO ─────────────────────────────────────────
    public class DocumentDto
    {
        public long DocumentID { get; set; }
        public long ProtocolID { get; set; }
        public string ProtocolTitle { get; set; } = string.Empty;
        public long UploadedBy { get; set; }
        public string UploaderName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string FileURL { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public string? ReviewNotes { get; set; }
        public long? ReviewedBy { get; set; }
        public string? ReviewerName { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }

    // ── Create (Admin / CTM uploads) ─────────────────────────
    public class CreateDocumentRequest
    {
        [Required(ErrorMessage = "Protocol is required.")]
        public long ProtocolID { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Document type is required.")]
        [MaxLength(100)]
        public string Type { get; set; } = string.Empty;
        // Valid: Protocol | Amendment | InformedConsentForm |
        //        InvestigatorBrochure | SafetyReport |
        //        MonitoringReport | RegulatorySubmission

        [Required(ErrorMessage = "Version is required.")]
        [MaxLength(50)]
        public string Version { get; set; } = string.Empty;

        [Required(ErrorMessage = "File URL is required.")]
        [MaxLength(500)]
        public string FileURL { get; set; } = string.Empty;
    }

    // ── Review (RO approves or rejects) ──────────────────────
    public class ReviewDocumentRequest
    {
        [Required(ErrorMessage = "Approve flag is required.")]
        public bool Approve { get; set; }

        [MaxLength(1000)]
        public string? ReviewNotes { get; set; }
    }

    // ── Filter ───────────────────────────────────────────────
    public class DocumentFilterDto
    {
        public long? ProtocolID { get; set; }
        public string? Type { get; set; }
        public string? Status { get; set; }
    }
}