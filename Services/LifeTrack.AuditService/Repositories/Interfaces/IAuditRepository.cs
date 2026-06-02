// ============================================================
// AuditService.API / Repositories / Interfaces / IAuditRepository.cs
// ============================================================

using LifeTrack.AuditService.DTOs;

namespace LifeTrack.AuditService.Repositories.Interfaces
{
    public interface IAuditRepository
    {
        Task<AuditPagedResult> GetLogsAsync(AuditFilterDto filter);
        Task LogAsync(CreateAuditLogRequest req);
    }
}