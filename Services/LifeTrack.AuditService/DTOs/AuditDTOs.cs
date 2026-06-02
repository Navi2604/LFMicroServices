// ============================================================
// AuditService.API / DTOs / AuditDTOs.cs
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace LifeTrack.AuditService.DTOs
{
    public class AuditLogDto
    {
        public long AuditID { get; set; }
        public long UserID { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public long? EntityID { get; set; }
        public string Details { get; set; } = string.Empty;
        public DateTime ActionTime { get; set; }
    }

    public class CreateAuditLogRequest
    {
        [Required]
        public long UserID { get; set; }

        [Required, MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;

        public long? EntityID { get; set; }

        [MaxLength(1000)]
        public string Details { get; set; } = string.Empty;
    }

    public class AuditFilterDto
    {
        public long? UserID { get; set; }
        public string? Action { get; set; }
        public string? EntityType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class AuditPagedResult
    {
        public List<AuditLogDto> Logs { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}