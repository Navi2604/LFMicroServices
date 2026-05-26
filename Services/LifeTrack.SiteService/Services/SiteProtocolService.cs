// ============================================================
// SiteService.API / Services / SiteProtocolService.cs
// WITH DELETE — Caching at repo level
// ============================================================

using LifeTrack.Shared.Wrappers;
using LifeTrack.SiteService.DTOs;
using LifeTrack.SiteService.Repositories.Interfaces;
using LifeTrack.SiteService.Services.Interfaces;

namespace LifeTrack.SiteService.Services
{
    public class SiteProtocolService : ISiteProtocolService
    {
        private readonly ISiteProtocolRepository _repo;

        public SiteProtocolService(ISiteProtocolRepository repo) => _repo = repo;

        // ✅ Caching is handled at repository level

        public async Task<ApiResponse<List<SiteProtocolDto>>> GetAllAsync(SiteProtocolFilterDto filter)
            => ApiResponse<List<SiteProtocolDto>>.Ok(await _repo.GetAllAsync(filter));

        public async Task<ApiResponse<SiteProtocolDto>> GetByIdAsync(long id)
        {
            var sp = await _repo.GetByIdAsync(id);
            return sp == null
                ? ApiResponse<SiteProtocolDto>.Fail("SiteProtocol not found.")
                : ApiResponse<SiteProtocolDto>.Ok(sp);
        }

        public async Task<ApiResponse<SiteProtocolDto>> CreateAsync(CreateSiteProtocolRequest req)
        {
            var sp = await _repo.CreateAsync(req);
            return ApiResponse<SiteProtocolDto>.Ok(sp, "SiteProtocol created successfully.");
        }

        public async Task<ApiResponse<bool>> UpdateStatusAsync(long id, string status)
        {
            var updated = await _repo.UpdateStatusAsync(id, status);
            return updated
                ? ApiResponse<bool>.Ok(true, "Status updated successfully.")
                : ApiResponse<bool>.Fail("SiteProtocol not found.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            var deleted = await _repo.DeleteAsync(id);
            return deleted
                ? ApiResponse<bool>.Ok(true, "SiteProtocol deleted.")
                : ApiResponse<bool>.Fail("SiteProtocol not found.");
        }
    }
}