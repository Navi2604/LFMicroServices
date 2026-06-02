// ============================================================
// UserService.API / DTOs / NotificationDTOs.cs
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace LifeTrack.UserService.DTOs
{
    public class NotificationDto
    {
        public long NotificationID { get; set; }
        public long UserID { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class CreateNotificationRequest
    {
        [Required(ErrorMessage = "User is required.")]
        public long UserID { get; set; }

        [Required(ErrorMessage = "Message is required.")]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;
    }

    public class NotificationFilterDto
    {
        public long? UserID { get; set; }
        public string? Status { get; set; }
        public string? Category { get; set; }
    }
}