// ============================================================
// UserService.API / Repositories / NotificationRepository.cs
// ============================================================

using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using LifeTrack.UserService.DTOs;
using LifeTrack.UserService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.UserService.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly LifeTrackDbContext _db;

        public NotificationRepository(LifeTrackDbContext db) => _db = db;

        public async Task<List<NotificationDto>> GetAllAsync(NotificationFilterDto filter)
        {
            var query = _db.Notifications.AsQueryable();

            if (filter.UserID.HasValue)
                query = query.Where(n => n.UserID == filter.UserID.Value);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(n => n.Status == filter.Status);

            if (!string.IsNullOrEmpty(filter.Category))
                query = query.Where(n => n.Category == filter.Category);

            return await query
                .OrderByDescending(n => n.CreatedDate)
                .Select(n => new NotificationDto
                {
                    NotificationID = n.NotificationID,
                    UserID = n.UserID,
                    Message = n.Message,
                    Category = n.Category,
                    Status = n.Status,
                    CreatedDate = n.CreatedDate
                })
                .ToListAsync();
        }

        public async Task<NotificationDto?> GetByIdAsync(long id)
        {
            var n = await _db.Notifications.FindAsync(id);
            if (n == null) return null;

            return new NotificationDto
            {
                NotificationID = n.NotificationID,
                UserID = n.UserID,
                Message = n.Message,
                Category = n.Category,
                Status = n.Status,
                CreatedDate = n.CreatedDate
            };
        }

        public async Task<NotificationDto> CreateAsync(CreateNotificationRequest req)
        {
            var n = new Notification
            {
                UserID = req.UserID,
                Message = req.Message,
                Category = req.Category,
                Status = "Unread",
                CreatedDate = DateTime.UtcNow
            };

            _db.Notifications.Add(n);
            await _db.SaveChangesAsync();

            return new NotificationDto
            {
                NotificationID = n.NotificationID,
                UserID = n.UserID,
                Message = n.Message,
                Category = n.Category,
                Status = n.Status,
                CreatedDate = n.CreatedDate
            };
        }

        public async Task<bool> MarkAsReadAsync(long id)
        {
            var n = await _db.Notifications.FindAsync(id);
            if (n == null) return false;

            n.Status = "Read";
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(long userId)
        {
            var notifications = await _db.Notifications
                .Where(n => n.UserID == userId && n.Status == "Unread")
                .ToListAsync();

            foreach (var n in notifications)
                n.Status = "Read";

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var n = await _db.Notifications.FindAsync(id);
            if (n == null) return false;

            _db.Notifications.Remove(n);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}