// ============================================================
// VisitService.API / Repositories / Interfaces / IVisitRepository.cs
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
        Task<bool> DeleteAsync(long id);
    }
}