// ============================================================
// VisitService.API / Services / VisitService.cs
// ============================================================

using LifeTrack.Shared.Helpers;
using LifeTrack.Shared.Wrappers;
using LifeTrack.VisitService.DTOs;
using LifeTrack.VisitService.Repositories.Interfaces;
using LifeTrack.VisitService.Services.Interfaces;

namespace LifeTrack.VisitService.Services
{
    public class VisitService : IVisitService
    {
        private readonly IVisitRepository _repo;
        private readonly AuditHttpClient _audit;

        public VisitService(IVisitRepository repo, AuditHttpClient audit)
        {
            _repo = repo;
            _audit = audit;
        }

        public async Task<ApiResponse<List<VisitDto>>> GetAllAsync(VisitFilterDto filter)
            => ApiResponse<List<VisitDto>>.Ok(await _repo.GetAllAsync(filter));

        public async Task<ApiResponse<VisitDto>> GetByIdAsync(long id)
        {
            var v = await _repo.GetByIdAsync(id);
            return v == null
                ? ApiResponse<VisitDto>.Fail("Visit not found.")
                : ApiResponse<VisitDto>.Ok(v);
        }

        public async Task<ApiResponse<VisitDto>> CreateAsync(CreateVisitRequest req)
        {
            var visit = await _repo.CreateAsync(req);

            _audit.Log("CREATE", "Visit", visit.VisitID,
                $"Visit scheduled for patient '{visit.PatientName}' " +
                $"on {visit.VisitDate:dd MMM yyyy}. Status: '{visit.Status}'.");

            return ApiResponse<VisitDto>.Ok(visit, "Visit scheduled successfully.");
        }

        public async Task<ApiResponse<bool>> UpdateStatusAsync(long id, string status)
        {
            var updated = await _repo.UpdateStatusAsync(id, status);

            if (updated)
                _audit.Log("UPDATE", "Visit", id,
                    $"Visit status updated to '{status}'.");

            return updated
                ? ApiResponse<bool>.Ok(true, "Visit status updated.")
                : ApiResponse<bool>.Fail("Visit not found.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            var deleted = await _repo.DeleteAsync(id);

            if (deleted)
                _audit.Log("DELETE", "Visit", id,
                    $"Visit ID {id} deleted.");

            return deleted
                ? ApiResponse<bool>.Ok(true, "Visit deleted successfully.")
                : ApiResponse<bool>.Fail("Visit not found.");
        }
    }
}