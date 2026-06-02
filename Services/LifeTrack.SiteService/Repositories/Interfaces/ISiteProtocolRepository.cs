// ============================================================
// SiteService.API / Repositories / Interfaces / ISiteProtocolRepository.cs
// ============================================================

using LifeTrack.SiteService.DTOs;

namespace LifeTrack.SiteService.Repositories.Interfaces
{
    public interface ISiteProtocolRepository
    {
        Task<List<SiteProtocolDto>> GetAllAsync(SiteProtocolFilterDto filter);
        Task<SiteProtocolDto?> GetByIdAsync(long id);
        Task<SiteProtocolDto> CreateAsync(CreateSiteProtocolRequest req);
        Task<bool> UpdateStatusAsync(long id, string status);
        Task<bool> DeleteAsync(long id);
    }
}