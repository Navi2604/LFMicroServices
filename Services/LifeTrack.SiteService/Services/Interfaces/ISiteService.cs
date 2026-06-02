// ============================================================
// SiteService.API / Services / Interfaces / ISiteService.cs
// ============================================================

using LifeTrack.Shared.Wrappers;
using LifeTrack.SiteService.DTOs;

namespace LifeTrack.SiteService.Services.Interfaces
{
    public interface ISiteService
    {
        Task<ApiResponse<List<SiteDto>>> GetAllAsync(SiteFilterDto filter);
        Task<ApiResponse<SiteDto>> GetByIdAsync(long id);
        Task<ApiResponse<SiteDto>> CreateAsync(CreateSiteRequest req);
        Task<ApiResponse<bool>> DeleteAsync(long id);
    }
}