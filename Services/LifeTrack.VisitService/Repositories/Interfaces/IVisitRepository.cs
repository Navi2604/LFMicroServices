// ============================================================
// VisitService.API / Repositories / Interfaces / IVisitRepository.cs
// WITH CACHE — No changes (caching at implementation level)
// ============================================================

using LifeTrack.VisitService.DTOs;

namespace LifeTrack.VisitService.Repositories.Interfaces
{
    public interface IVisitRepository
    {
        Task<List<VisitDto>> GetAllAsync(VisitFilterDto filter);
        Task<VisitDto?> GetByIdAsync(long id);
        Task<VisitDto> CreateAsync(CreateVisitRequest req);
        Task<bool> UpdateStatusAsync(long id, string status);
        Task<bool> DeleteAsync(long id);   // ✅ Only deletes Scheduled visits
    }
}