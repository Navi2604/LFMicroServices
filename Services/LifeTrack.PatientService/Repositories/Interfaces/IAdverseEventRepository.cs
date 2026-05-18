// ============================================================
// PatientService.API / Repositories / Interfaces / IAdverseEventRepository.cs
// ============================================================

using LifeTrack.PatientService.DTOs;

namespace LifeTrack.PatientService.Repositories.Interfaces
{
    public interface IAdverseEventRepository
    {
        Task<List<AdverseEventDto>> GetAllAsync(AdverseEventFilterDto filter);
        Task<AdverseEventDto> CreateAsync(CreateAdverseEventRequest req);
        Task<bool> UpdateStatusAsync(long id, string status);
        Task<bool> DeleteAsync(long id);
    }
}