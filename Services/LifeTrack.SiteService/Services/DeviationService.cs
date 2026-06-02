// ============================================================
// SiteService.API / Services / DeviationService.cs
// ============================================================

using LifeTrack.Shared.Helpers;
using LifeTrack.Shared.Wrappers;
using LifeTrack.SiteService.DTOs;
using LifeTrack.SiteService.Repositories.Interfaces;
using LifeTrack.SiteService.Services.Interfaces;

namespace LifeTrack.SiteService.Services
{
    public class DeviationService : IDeviationService
    {
        private readonly IDeviationRepository _repo;
        private readonly AuditHttpClient _audit;

        public DeviationService(IDeviationRepository repo, AuditHttpClient audit)
        {
            _repo = repo;
            _audit = audit;
        }

        public async Task<ApiResponse<List<DeviationDto>>> GetAllAsync(DeviationFilterDto filter)
            => ApiResponse<List<DeviationDto>>.Ok(await _repo.GetAllAsync(filter));

        public async Task<ApiResponse<DeviationDto>> GetByIdAsync(long id)
        {
            var d = await _repo.GetByIdAsync(id);
            return d == null
                ? ApiResponse<DeviationDto>.Fail("Deviation not found.")
                : ApiResponse<DeviationDto>.Ok(d);
        }

        public async Task<ApiResponse<DeviationDto>> CreateAsync(CreateDeviationRequest req)
        {
            var d = await _repo.CreateAsync(req);

            _audit.Log("CREATE", "Deviation", d.DeviationID,
                $"Deviation reported for SiteProtocol ID {d.SiteProtocolID}. " +
                $"Severity: '{d.Severity}'.");

            return ApiResponse<DeviationDto>.Ok(d, "Deviation reported successfully.");
        }

        public async Task<ApiResponse<bool>> UpdateStatusAsync(long id, string status)
        {
            var updated = await _repo.UpdateStatusAsync(id, status);

            if (updated)
                _audit.Log("UPDATE", "Deviation", id,
                    $"Deviation status updated to '{status}'.");

            return updated
                ? ApiResponse<bool>.Ok(true, "Status updated successfully.")
                : ApiResponse<bool>.Fail("Deviation not found.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            var deleted = await _repo.DeleteAsync(id);

            if (deleted)
                _audit.Log("DELETE", "Deviation", id,
                    $"Deviation ID {id} deleted.");

            return deleted
                ? ApiResponse<bool>.Ok(true, "Deviation deleted.")
                : ApiResponse<bool>.Fail("Deviation not found.");
        }
    }
}