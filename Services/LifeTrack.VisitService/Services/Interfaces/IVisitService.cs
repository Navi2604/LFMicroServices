// ============================================================
// VisitService.API / Services / Interfaces / IVisitService.cs
// WITH CACHE — No changes (caching at repo level)
// ============================================================

using LifeTrack.Shared.Wrappers;
using LifeTrack.VisitService.DTOs;

namespace LifeTrack.VisitService.Services.Interfaces
{
    public interface IVisitService
    {
        Task<ApiResponse<List<VisitDto>>> GetAllAsync(VisitFilterDto filter);
        Task<ApiResponse<VisitDto>> GetByIdAsync(long id);
        Task<ApiResponse<VisitDto>> CreateAsync(CreateVisitRequest req);
        Task<ApiResponse<bool>> UpdateStatusAsync(long id, string status);
        Task<ApiResponse<bool>> DeleteAsync(long id);   // ✅ Only deletes Scheduled visits
    }
}