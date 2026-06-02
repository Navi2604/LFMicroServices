// ============================================================
// UserService.API / Services / NotificationService.cs
// ============================================================

using LifeTrack.Shared.Wrappers;
using LifeTrack.UserService.DTOs;
using LifeTrack.UserService.Repositories.Interfaces;
using LifeTrack.UserService.Services.Interfaces;

namespace LifeTrack.UserService.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;

        public NotificationService(INotificationRepository repo) => _repo = repo;

        public async Task<ApiResponse<List<NotificationDto>>> GetAllAsync(NotificationFilterDto filter)
            => ApiResponse<List<NotificationDto>>.Ok(await _repo.GetAllAsync(filter));

        public async Task<ApiResponse<NotificationDto>> GetByIdAsync(long id)
        {
            var n = await _repo.GetByIdAsync(id);
            return n == null
                ? ApiResponse<NotificationDto>.Fail($"Notification {id} not found.")
                : ApiResponse<NotificationDto>.Ok(n);
        }

        public async Task<ApiResponse<NotificationDto>> CreateAsync(CreateNotificationRequest req)
        {
            var n = await _repo.CreateAsync(req);
            return ApiResponse<NotificationDto>.Ok(n, "Notification created.");
        }

        public async Task<ApiResponse<bool>> MarkAsReadAsync(long id)
        {
            var updated = await _repo.MarkAsReadAsync(id);
            return updated
                ? ApiResponse<bool>.Ok(true, "Notification marked as read.")
                : ApiResponse<bool>.Fail("Notification not found.");
        }

        public async Task<ApiResponse<bool>> MarkAllAsReadAsync(long userId)
        {
            await _repo.MarkAllAsReadAsync(userId);
            return ApiResponse<bool>.Ok(true, "All notifications marked as read.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            var deleted = await _repo.DeleteAsync(id);
            return deleted
                ? ApiResponse<bool>.Ok(true, "Notification deleted.")
                : ApiResponse<bool>.Fail("Notification not found.");
        }
    }
}