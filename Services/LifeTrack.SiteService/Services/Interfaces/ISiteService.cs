// ============================================================
// SiteService.API / Services / Interfaces / ISiteService.cs
// DELETE METHOD REMOVED — Updated
// ============================================================

using LifeTrack.SiteService.DTOs;
using LifeTrack.Shared.Wrappers;

namespace LifeTrack.SiteService.Services.Interfaces
{
    public interface ISiteService
    {
        Task<ApiResponse<List<SiteDto>>> GetAllAsync(SiteFilterDto filter);
        Task<ApiResponse<SiteDto>> GetByIdAsync(long id);
        Task<ApiResponse<SiteDto>> CreateAsync(CreateSiteRequest req);
        Task<ApiResponse<SiteDto>> UpdateAsync(long id, UpdateSiteRequest req);
        // ❌ DELETE REMOVED
    }
}