using LifeTrack.Shared.Wrappers;
using LifeTrack.SiteService.DTOs;

namespace LifeTrack.SiteService.Services.Interfaces
{
    public interface ISiteService
    {
        Task<ApiResponse<List<SiteDto>>> GetAllAsync();
        Task<ApiResponse<SiteDto>> GetByIdAsync(long id);
        Task<ApiResponse<SiteDto>> CreateAsync(
            CreateSiteRequest req);
        Task<ApiResponse<SiteDto>> UpdateAsync(
            long id, CreateSiteRequest req);
        Task<ApiResponse<bool>> DeleteAsync(long id);
    }
}