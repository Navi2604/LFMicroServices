// ============================================================
// AuditService.API / Services / AuditService.cs
// ============================================================

using LifeTrack.AuditService.DTOs;
using LifeTrack.AuditService.Repositories.Interfaces;
using LifeTrack.AuditService.Services.Interfaces;
using LifeTrack.Shared.Wrappers;

namespace LifeTrack.AuditService.Services
{
    public class AuditService : IAuditService
    {
        private readonly IAuditRepository _repo;

        public AuditService(IAuditRepository repo) => _repo = repo;

        public async Task<ApiResponse<AuditPagedResult>> GetLogsAsync(AuditFilterDto filter)
        {
            var result = await _repo.GetLogsAsync(filter);
            return ApiResponse<AuditPagedResult>.Ok(result);
        }

        public async Task<ApiResponse<bool>> LogAsync(CreateAuditLogRequest req)
        {
            await _repo.LogAsync(req);
            return ApiResponse<bool>.Ok(true, "Audit log recorded.");
        }
    }
}