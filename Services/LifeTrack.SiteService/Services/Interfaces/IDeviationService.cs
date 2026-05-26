// ============================================================
// SiteService.API / Services / Interfaces / IDeviationService.cs
// ============================================================

using LifeTrack.Shared.Wrappers;
using LifeTrack.SiteService.DTOs;

namespace LifeTrack.SiteService.Services.Interfaces
{
    public interface IDeviationService
    {
        Task<ApiResponse<List<DeviationDto>>> GetAllAsync(DeviationFilterDto filter);
        Task<ApiResponse<DeviationDto>> GetByIdAsync(long id);
        Task<ApiResponse<DeviationDto>> CreateAsync(CreateDeviationRequest req);
        Task<ApiResponse<bool>> UpdateStatusAsync(long id, string status, string updaterRole);
        Task<ApiResponse<bool>> DeleteAsync(long id);
    }
}