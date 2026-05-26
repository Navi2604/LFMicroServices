// ============================================================
// SiteService.API / Repositories / Interfaces / IDeviationRepository.cs
// ============================================================

using LifeTrack.SiteService.DTOs;

namespace LifeTrack.SiteService.Repositories.Interfaces
{
    public interface IDeviationRepository
    {
        Task<List<DeviationDto>> GetAllAsync(DeviationFilterDto filter);
        Task<DeviationDto?> GetByIdAsync(long id);
        Task<DeviationDto> CreateAsync(CreateDeviationRequest req);
        Task<bool> UpdateStatusAsync(long id, string status, string updaterRole);
        Task<bool> DeleteAsync(long id);
    }
}