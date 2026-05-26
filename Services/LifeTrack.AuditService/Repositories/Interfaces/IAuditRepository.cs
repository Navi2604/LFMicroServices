// ============================================================
// AuditService.API / Repositories / Interfaces / IAuditRepository.cs
// NO CHANGES — Audit doesn't have Delete
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