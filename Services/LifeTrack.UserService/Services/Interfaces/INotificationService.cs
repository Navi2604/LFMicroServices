// ============================================================
// UserService.API / Services / Interfaces / INotificationService.cs
// ============================================================

using LifeTrack.Shared.Wrappers;
using LifeTrack.UserService.DTOs;

namespace LifeTrack.UserService.Services.Interfaces
{
    public interface INotificationService
    {
        Task<ApiResponse<List<NotificationDto>>> GetAllAsync(NotificationFilterDto filter);
        Task<ApiResponse<NotificationDto>> GetByIdAsync(long id);
        Task<ApiResponse<NotificationDto>> CreateAsync(CreateNotificationRequest req);
        Task<ApiResponse<bool>> MarkAsReadAsync(long id);
        Task<ApiResponse<bool>> MarkAllAsReadAsync(long userId);
        Task<ApiResponse<bool>> DeleteAsync(long id);
    }
}