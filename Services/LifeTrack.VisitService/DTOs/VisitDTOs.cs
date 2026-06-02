// ============================================================
// VisitService.API / DTOs / VisitDTOs.cs
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace LifeTrack.VisitService.DTOs
{
    public class VisitDto
    {
        public long VisitID { get; set; }
        public long EnrollmentID { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string ProtocolTitle { get; set; } = string.Empty;
        public DateTime VisitDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class CreateVisitRequest
    {
        [Required(ErrorMessage = "Enrollment is required.")]
        public long EnrollmentID { get; set; }

        [Required(ErrorMessage = "Visit date is required.")]
        public DateTime VisitDate { get; set; }

        public string Status { get; set; } = "Scheduled";

        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty;
    }

    public class UpdateVisitStatusRequest
    {
        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = string.Empty;
    }

    public class VisitFilterDto
    {
        public long? EnrollmentID { get; set; }
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}