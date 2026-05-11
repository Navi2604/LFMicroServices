namespace LifeTrack.AuditService.Models
{
    public class AuditLog
    {
        public long AuditID { get; set; }
        public string UserID { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}