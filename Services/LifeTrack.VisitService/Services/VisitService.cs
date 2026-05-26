// ============================================================
// VisitService.API / Services / VisitService.cs
// WITH CACHING — Caching at repo level
// ============================================================

using LifeTrack.Shared.Wrappers;
using LifeTrack.VisitService.DTOs;
using LifeTrack.VisitService.Repositories.Interfaces;
using LifeTrack.VisitService.Services.Interfaces;

namespace LifeTrack.VisitService.Services
{
    public class VisitService : IVisitService
    {
        private readonly IVisitRepository _repo;

        public VisitService(IVisitRepository repo) => _repo = repo;

        // ✅ Caching is handled at repository level

        public async Task<ApiResponse<List<VisitDto>>> GetAllAsync(VisitFilterDto filter)
            => ApiResponse<List<VisitDto>>.Ok(await _repo.GetAllAsync(filter));

        public async Task<ApiResponse<VisitDto>> GetByIdAsync(long id)
        {
            var visit = await _repo.GetByIdAsync(id);
            return visit == null
                ? ApiResponse<VisitDto>.Fail($"Visit {id} not found.")
                : ApiResponse<VisitDto>.Ok(visit);
        }

        public async Task<ApiResponse<VisitDto>> CreateAsync(CreateVisitRequest req)
        {
            var visit = await _repo.CreateAsync(req);
            return ApiResponse<VisitDto>.Ok(visit, "Visit created successfully.");
        }

        public async Task<ApiResponse<bool>> UpdateStatusAsync(long id, string status)
        {
            var updated = await _repo.UpdateStatusAsync(id, status);
            return updated
                ? ApiResponse<bool>.Ok(true, "Visit status updated successfully.")
                : ApiResponse<bool>.Fail($"Visit {id} not found.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            var deleted = await _repo.DeleteAsync(id);
            return deleted
                ? ApiResponse<bool>.Ok(true, "Visit deleted successfully.")
                : ApiResponse<bool>.Fail("Visit not found or cannot be deleted (only Scheduled visits can be deleted).");
        }
    }
}