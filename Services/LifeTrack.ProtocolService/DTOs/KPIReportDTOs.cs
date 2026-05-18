// ============================================================
// ProtocolService.API / DTOs / KPIReportDTOs.cs
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace LifeTrack.ProtocolService.DTOs
{
    public class KPIReportDto
    {
        public long ReportID { get; set; }
        public long ProtocolID { get; set; }
        public string ProtocolTitle { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public decimal EnrollmentRate { get; set; }
        public decimal DropoutRate { get; set; }
        public int AECount { get; set; }
        public DateTime GeneratedDate { get; set; }
    }

    public class CreateKPIReportRequest
    {
        [Required(ErrorMessage = "Protocol is required.")]
        public long ProtocolID { get; set; }

        [MaxLength(200)]
        public string Scope { get; set; } = string.Empty;

        [Range(0, 100, ErrorMessage = "Enrollment rate must be between 0 and 100.")]
        public decimal EnrollmentRate { get; set; }

        [Range(0, 100, ErrorMessage = "Dropout rate must be between 0 and 100.")]
        public decimal DropoutRate { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "AE count must be 0 or greater.")]
        public int AECount { get; set; }
    }

    public class KPIReportFilterDto
    {
        public long? ProtocolID { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}