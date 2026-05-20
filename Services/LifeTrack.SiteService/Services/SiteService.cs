// SiteService.cs
using LifeTrack.Shared.Wrappers;
using LifeTrack.SiteService.DTOs;
using LifeTrack.SiteService.Repositories.Interfaces;
using LifeTrack.SiteService.Services.Interfaces;

namespace LifeTrack.SiteService.Services
{
    public class SiteService : ISiteService
    {
        private readonly ISiteRepository _repo;
        public SiteService(ISiteRepository repo) => _repo = repo;

        public async Task<ApiResponse<List<SiteDto>>> GetAllAsync(SiteFilterDto filter)
            => ApiResponse<List<SiteDto>>.Ok(await _repo.GetAllAsync(filter));

        public async Task<ApiResponse<SiteDto>> GetByIdAsync(long id)
        {
            var s = await _repo.GetByIdAsync(id);
            return s == null ? ApiResponse<SiteDto>.Fail("Site not found.") : ApiResponse<SiteDto>.Ok(s);
        }

        public async Task<ApiResponse<SiteDto>> CreateAsync(CreateSiteRequest req)
        {
            var s = await _repo.CreateAsync(req);
            return ApiResponse<SiteDto>.Ok(s, "Site created successfully.");
        }

        public async Task<ApiResponse<SiteDto>> UpdateAsync(long id, UpdateSiteRequest req)
        {
            var s = await _repo.UpdateAsync(id, req);
            return s == null
                ? ApiResponse<SiteDto>.Fail("Site not found.")
                : ApiResponse<SiteDto>.Ok(s, "Site updated successfully.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            var deleted = await _repo.DeleteAsync(id);
            return deleted
                ? ApiResponse<bool>.Ok(true, "Site deleted successfully.")
                : ApiResponse<bool>.Fail("Site not found.");
        }
    }
}