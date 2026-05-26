// ============================================================
// UserService.API / Repositories / NotificationRepository.cs
// WITH CACHING — Updated
// ============================================================

using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using LifeTrack.UserService.DTOs;
using LifeTrack.UserService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LifeTrack.UserService.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly LifeTrackDbContext _db;
        private readonly IMemoryCache _cache;
        private const string NOTIFICATION_CACHE_KEY = "notifications_{0}_{1}_{2}";
        private const string NOTIFICATION_ID_CACHE_KEY = "notification_{0}";
        private const int CACHE_DURATION_MINUTES = 5;

        public NotificationRepository(LifeTrackDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<List<NotificationDto>> GetAllAsync(NotificationFilterDto filter)
        {
            string cacheKey = string.Format(
                NOTIFICATION_CACHE_KEY,
                filter.UserID?.ToString() ?? "null",
                filter.Status ?? "null",
                filter.Category ?? "null"
            );

            if (_cache.TryGetValue(cacheKey, out List<NotificationDto>? cachedNotifications))
                return cachedNotifications!;

            var query = _db.Notifications.AsQueryable();

            if (filter.UserID.HasValue)
                query = query.Where(n => n.UserID == filter.UserID.Value);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(n => n.Status == filter.Status);

            if (!string.IsNullOrEmpty(filter.Category))
                query = query.Where(n => n.Category == filter.Category);

            var result = await query
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

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            return result;
        }

        public async Task<NotificationDto?> GetByIdAsync(long id)
        {
            string cacheKey = string.Format(NOTIFICATION_ID_CACHE_KEY, id);

            if (_cache.TryGetValue(cacheKey, out NotificationDto? cachedNotification))
                return cachedNotification;

            var n = await _db.Notifications.FindAsync(id);
            if (n == null) return null;

            var dto = new NotificationDto
            {
                NotificationID = n.NotificationID,
                UserID = n.UserID,
                Message = n.Message,
                Category = n.Category,
                Status = n.Status,
                CreatedDate = n.CreatedDate
            };

            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            return dto;
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

            InvalidateCache(req.UserID);

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

            InvalidateCache(n.UserID);
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

            InvalidateCache(userId);
            return true;
        }

        // ❌ NO DELETE METHOD — Removed

        private void InvalidateCache(long userId)
        {
            string cacheKey = string.Format(NOTIFICATION_CACHE_KEY, userId, "*", "*");
            _cache.Remove(cacheKey);
        }
    }
}