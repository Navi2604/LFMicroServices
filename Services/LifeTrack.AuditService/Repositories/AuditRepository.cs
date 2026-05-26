// ============================================================
// AuditService.API / Repositories / AuditRepository.cs
// WITH CACHING
// ============================================================

using LifeTrack.AuditService.DTOs;
using LifeTrack.AuditService.Repositories.Interfaces;
using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LifeTrack.AuditService.Repositories
{
    public class AuditRepository : IAuditRepository
    {
        private readonly AuditDbContext _db;
        private readonly IMemoryCache _cache;
        private const string AUDIT_CACHE_KEY = "audit_logs_{0}_{1}_{2}_{3}_{4}_{5}";
        private const int CACHE_DURATION_MINUTES = 10;

        public AuditRepository(AuditDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<AuditPagedResult> GetLogsAsync(AuditFilterDto filter)
        {
            // ✅ CREATE CACHE KEY FROM FILTER
            string cacheKey = string.Format(
                AUDIT_CACHE_KEY,
                filter.UserID?.ToString() ?? "null",
                filter.Action ?? "null",
                filter.EntityType ?? "null",
                filter.FromDate?.ToString("yyyy-MM-dd") ?? "null",
                filter.ToDate?.ToString("yyyy-MM-dd") ?? "null",
                $"{filter.Page}_{filter.PageSize}"
            );

            // ✅ CHECK CACHE FIRST
            if (_cache.TryGetValue(cacheKey, out AuditPagedResult? cachedResult))
                return cachedResult!;

            var query = _db.AuditLogs.AsQueryable();

            if (filter.UserID.HasValue)
                query = query.Where(a => a.UserID == filter.UserID.Value);

            if (!string.IsNullOrEmpty(filter.Action))
                query = query.Where(a => a.Action.Contains(filter.Action));

            if (!string.IsNullOrEmpty(filter.EntityType))
                query = query.Where(a => a.EntityType == filter.EntityType);

            if (filter.FromDate.HasValue)
                query = query.Where(a => a.ActionTime >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(a => a.ActionTime <= filter.ToDate.Value);

            var total = await query.CountAsync();

            var logs = await query
                .OrderByDescending(a => a.ActionTime)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(a => new AuditLogDto
                {
                    AuditID = a.AuditID,
                    UserID = a.UserID,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityID = a.EntityID,
                    Details = a.Details,
                    ActionTime = a.ActionTime
                })
                .ToListAsync();

            var result = new AuditPagedResult
            {
                Logs = logs,
                Total = total,
                Page = filter.Page,
                PageSize = filter.PageSize
            };

            // ✅ STORE IN CACHE
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            return result;
        }

        public async Task LogAsync(CreateAuditLogRequest req)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                UserID = req.UserID,
                Action = req.Action,
                EntityType = req.EntityType,
                EntityID = req.EntityID,
                Details = req.Details,
                ActionTime = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            // ✅ INVALIDATE ALL AUDIT CACHES (new log added)
            InvalidateAllAuditCache();
        }

        private void InvalidateAllAuditCache()
        {
            // Clear all audit log caches (brute force approach)
            for (int i = 0; i < 100; i++)
            {
                string cacheKey = string.Format(
                    AUDIT_CACHE_KEY,
                    $"*{i}", $"*{i}", $"*{i}", $"*{i}", $"*{i}", $"*{i}"
                );
                _cache.Remove(cacheKey);
            }
        }
    }
}