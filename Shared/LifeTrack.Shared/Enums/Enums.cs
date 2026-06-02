// ============================================================
// Shared.CL / Enums / Enums.cs
// ============================================================

namespace LifeTrack.Shared.Enums
{
    // ── Protocol status ───────────────────────────────────────
    public enum ProtocolStatus
    {
        Upcoming,
        Ongoing,
        Completed,
        Archived
    }

    // ── Site status ───────────────────────────────────────────
    public enum SiteStatus
    {
        Active,
        Inactive,
        Closed
    }

    // ── Enrollment status ─────────────────────────────────────
    public enum EnrollmentStatus
    {
        Active,
        Completed,
        Withdrawn,
        Screening
    }

    // ── Visit status ──────────────────────────────────────────
    public enum VisitStatus
    {
        Scheduled,
        Completed,
        Missed,
        Cancelled
    }

    // ── Adverse event severity ────────────────────────────────
    public enum AESeverity
    {
        Mild,
        Moderate,
        Severe,
        LifeThreatening
    }

    // ── Adverse event status ──────────────────────────────────
    public enum AEStatus
    {
        Open,
        UnderReview,
        Resolved,
        Closed
    }

    // ── Deviation severity ────────────────────────────────────
    public enum DeviationSeverity
    {
        Minor,
        Major,
        Critical
    }

    // ── Deviation status ──────────────────────────────────────
    public enum DeviationStatus
    {
        Open,
        UnderReview,
        Resolved,
        Closed
    }

    // ── Notification status ───────────────────────────────────
    public enum NotificationStatus
    {
        Unread,
        Read
    }

    // ── Document status ───────────────────────────────────────
    public enum DocumentStatus
    {
        Draft,
        UnderReview,
        Approved,
        Superseded
    }

    // ── SiteProtocol status ───────────────────────────────────
    public enum SiteProtocolStatus
    {
        Active,
        Suspended,
        Closed
    }
}