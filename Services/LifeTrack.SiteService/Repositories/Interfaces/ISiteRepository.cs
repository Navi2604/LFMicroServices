// ============================================================
// SiteService.API / Repositories / Interfaces / ISiteRepository.cs
// DELETE METHOD REMOVED — Updated
// ============================================================

using LifeTrack.SiteService.DTOs;

namespace LifeTrack.SiteService.Repositories.Interfaces
{
    public interface ISiteRepository
    {
        Task<List<SiteDto>> GetAllAsync(SiteFilterDto filter);
        Task<SiteDto?> GetByIdAsync(long id);
        Task<SiteDto> CreateAsync(CreateSiteRequest req);
        Task<SiteDto?> UpdateAsync(long id, UpdateSiteRequest req);
        // ❌ DELETE REMOVED
    }
}