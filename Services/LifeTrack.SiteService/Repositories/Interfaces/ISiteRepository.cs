// ============================================================
// SiteService.API / Repositories / Interfaces / ISiteRepository.cs
// ============================================================
using LifeTrack.SiteService.DTOs;
namespace LifeTrack.SiteService.Repositories.Interfaces
{
    public interface ISiteRepository
    {
        Task<List<SiteDto>> GetAllAsync(SiteFilterDto filter);
        Task<SiteDto?> GetByIdAsync(long id);
        Task<SiteDto> CreateAsync(CreateSiteRequest req);
        Task<bool> DeleteAsync(long id);
    }
}