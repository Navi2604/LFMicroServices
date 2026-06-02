// ============================================================
// AuditService.API / Services / Interfaces / IAuditService.cs
// ============================================================

using LifeTrack.AuditService.DTOs;
using LifeTrack.Shared.Wrappers;

namespace LifeTrack.AuditService.Services.Interfaces
{
    public interface IAuditService
    {
        Task<ApiResponse<AuditPagedResult>> GetLogsAsync(AuditFilterDto filter);
        Task<ApiResponse<bool>> LogAsync(CreateAuditLogRequest req);
    }
}