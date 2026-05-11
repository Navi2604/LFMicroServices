namespace LifeTrack.Shared.Models
{
    public class User
    {
        public long UserID { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RoleID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class Role
    {
        public int RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }

    public class Patient
    {
        public long PatientID { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime DOB { get; set; }
        public string ContactInfo { get; set; } = string.Empty;
        public string EnrollmentStatus { get; set; } = "Pending";
        public long? EnrolledBy { get; set; }
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }
    }

    public class Protocol
    {
        public long ProtocolID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public long? InvestigatorID { get; set; }
        public string? PhasesJson { get; set; }
    }

    public class Site
    {
        public long SiteID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public long InvestigatorID { get; set; }
        public long? ProtocolID { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class Visit
    {
        public long VisitID { get; set; }
        public long PatientID { get; set; }
        public long ProtocolID { get; set; }
        public DateTime VisitDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class AdverseEvent
    {
        public long EventID { get; set; }
        public long PatientID { get; set; }
        public long ProtocolID { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ReportedDate { get; set; } = DateTime.UtcNow;
    }

    public class Deviation
    {
        public long DeviationID { get; set; }
        public long ProtocolID { get; set; }
        public long SiteID { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class Notification
    {
        public long NotificationID { get; set; }
        public long UserID { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}