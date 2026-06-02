// ============================================================
// PatientService.API / Services / Interfaces / IAdverseEventService.cs
// ============================================================

using LifeTrack.PatientService.DTOs;
using LifeTrack.Shared.Wrappers;

namespace LifeTrack.PatientService.Services.Interfaces
{
    public interface IAdverseEventService
    {
        Task<ApiResponse<List<AdverseEventDto>>> GetAllAsync(AdverseEventFilterDto filter);
        Task<ApiResponse<AdverseEventDto>> CreateAsync(CreateAdverseEventRequest req);
        Task<ApiResponse<bool>> UpdateStatusAsync(long id, string status);
        Task<ApiResponse<bool>> DeleteAsync(long id);
    }
}