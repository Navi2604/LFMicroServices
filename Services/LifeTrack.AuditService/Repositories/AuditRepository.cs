// ============================================================
// AuditService.API / Repositories / AuditRepository.cs
// ============================================================

using LifeTrack.AuditService.DTOs;
using LifeTrack.AuditService.Repositories.Interfaces;
using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.AuditService.Repositories
{
    public class AuditRepository : IAuditRepository
    {
        private readonly AuditDbContext _db;

        public AuditRepository(AuditDbContext db) => _db = db;

        public async Task<AuditPagedResult> GetLogsAsync(AuditFilterDto filter)
        {
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

            return new AuditPagedResult
            {
                Logs = logs,
                Total = total,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
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
        }
    }
}