namespace LifeTrack.Shared.DTOs
{
    public class AuditLogRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}