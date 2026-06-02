// ============================================================
// PatientService.API / DTOs / PatientDTOs.cs
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace LifeTrack.PatientService.DTOs
{
    // ── Patient ───────────────────────────────────────────────

    public class PatientDto
    {
        public long PatientID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DOB { get; set; } = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class CreatePatientRequest
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of birth is required.")]
        public DateTime DOB { get; set; }

        [MaxLength(200)]
        public string ContactInfo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; set; } = string.Empty;
    }

    public class PatientFilterDto
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public long? SiteProtocolID { get; set; }
        public string? EnrollmentStatus { get; set; }
    }

    // ── Enrollment ────────────────────────────────────────────

    public class EnrollmentDto
    {
        public long EnrollmentID { get; set; }
        public long PatientID { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public long SiteProtocolID { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public string ProtocolTitle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime EnrollmentDate { get; set; }
        public string? WithdrawalReason { get; set; }
    }

    public class EnrollPatientRequest
    {
        [Required(ErrorMessage = "Patient is required.")]
        public long PatientID { get; set; }

        [Required(ErrorMessage = "SiteProtocol is required.")]
        public long SiteProtocolID { get; set; }

        public DateTime? ConsentDate { get; set; }
    }

    public class UpdateEnrollmentStatusRequest
    {
        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? WithdrawalReason { get; set; }
    }

    public class EnrollmentFilterDto
    {
        public long? PatientID { get; set; }
        public long? SiteProtocolID { get; set; }
        public string? Status { get; set; }
    }

    // ── Adverse Event ─────────────────────────────────────────

    public class AdverseEventDto
    {
        public long EventID { get; set; }
        public long PatientID { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public long ProtocolID { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ReportedDate { get; set; }
    }

    public class CreateAdverseEventRequest
    {
        [Required(ErrorMessage = "Patient is required.")]
        public long PatientID { get; set; }

        [Required(ErrorMessage = "Protocol is required.")]
        public long ProtocolID { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Severity is required.")]
        public string Severity { get; set; } = string.Empty;
    }

    public class UpdateAdverseEventStatusRequest
    {
        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = string.Empty;
    }

    public class AdverseEventFilterDto
    {
        public long? PatientID { get; set; }
        public long? ProtocolID { get; set; }
        public string? Severity { get; set; }
        public string? Status { get; set; }
    }
}