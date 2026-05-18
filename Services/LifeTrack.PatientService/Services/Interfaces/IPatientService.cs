// ============================================================
// PatientService.API / Services / Interfaces / IPatientService.cs
// ============================================================

using LifeTrack.PatientService.DTOs;
using LifeTrack.Shared.Wrappers;

namespace LifeTrack.PatientService.Services.Interfaces
{
    public interface IPatientService
    {
        Task<ApiResponse<List<PatientDto>>> GetAllAsync(PatientFilterDto filter);
        Task<ApiResponse<PatientDto>> GetByIdAsync(long id);
        Task<ApiResponse<PatientDto>> CreateAsync(CreatePatientRequest req);
        Task<ApiResponse<bool>> DeleteAsync(long id);
    }
}