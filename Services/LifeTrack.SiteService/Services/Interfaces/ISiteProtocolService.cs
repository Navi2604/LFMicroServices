// ============================================================
// SiteService.API / Services / Interfaces / ISiteProtocolService.cs
// WITH DELETE — No changes (caching at repo level)
// ============================================================

using LifeTrack.Shared.Wrappers;
using LifeTrack.SiteService.DTOs;

namespace LifeTrack.SiteService.Services.Interfaces
{
    public interface ISiteProtocolService
    {
        Task<ApiResponse<List<SiteProtocolDto>>> GetAllAsync(SiteProtocolFilterDto filter);
        Task<ApiResponse<SiteProtocolDto>> GetByIdAsync(long id);
        Task<ApiResponse<SiteProtocolDto>> CreateAsync(CreateSiteProtocolRequest req);
        Task<ApiResponse<bool>> UpdateStatusAsync(long id, string status);
        Task<ApiResponse<bool>> DeleteAsync(long id);   // ✅ Allowed (unassigns investigator)
    }
}