// ============================================================
// SiteService.API / Services / SiteService.cs
// ============================================================

using LifeTrack.Shared.Helpers;
using LifeTrack.Shared.Wrappers;
using LifeTrack.SiteService.DTOs;
using LifeTrack.SiteService.Repositories.Interfaces;
using LifeTrack.SiteService.Services.Interfaces;

namespace LifeTrack.SiteService.Services
{
    public class SiteService : ISiteService
    {
        private readonly ISiteRepository _repo;
        private readonly AuditHttpClient _audit;

        public SiteService(ISiteRepository repo, AuditHttpClient audit)
        {
            _repo = repo;
            _audit = audit;
        }

        public async Task<ApiResponse<List<SiteDto>>> GetAllAsync(SiteFilterDto filter)
            => ApiResponse<List<SiteDto>>.Ok(await _repo.GetAllAsync(filter));

        public async Task<ApiResponse<SiteDto>> GetByIdAsync(long id)
        {
            var s = await _repo.GetByIdAsync(id);
            return s == null
                ? ApiResponse<SiteDto>.Fail("Site not found.")
                : ApiResponse<SiteDto>.Ok(s);
        }

        public async Task<ApiResponse<SiteDto>> CreateAsync(CreateSiteRequest req)
        {
            var site = await _repo.CreateAsync(req);

            _audit.Log("CREATE", "Site", site.SiteID,
                $"Site '{site.Name}' at '{site.Location}' created with status '{site.Status}'.");

            return ApiResponse<SiteDto>.Ok(site, "Site created successfully.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            var existing = await _repo.GetByIdAsync(id);
            var deleted = await _repo.DeleteAsync(id);

            if (deleted)
                _audit.Log("DELETE", "Site", id,
                    $"Site '{existing?.Name}' deleted.");

            return deleted
                ? ApiResponse<bool>.Ok(true, "Site deleted successfully.")
                : ApiResponse<bool>.Fail("Site not found.");
        }
    }
}