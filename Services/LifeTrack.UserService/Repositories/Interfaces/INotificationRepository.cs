// ============================================================
// UserService.API / Repositories / Interfaces / INotificationRepository.cs
// NO DELETE METHOD — Updated
// ============================================================

using LifeTrack.UserService.DTOs;

namespace LifeTrack.UserService.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        Task<List<NotificationDto>> GetAllAsync(NotificationFilterDto filter);
        Task<NotificationDto?> GetByIdAsync(long id);
        Task<NotificationDto> CreateAsync(CreateNotificationRequest req);
        Task<bool> MarkAsReadAsync(long id);
        Task<bool> MarkAllAsReadAsync(long userId);
        // ❌ DELETE REMOVED
    }
}