// ============================================================
// PatientService.API / Services / Interfaces / IEnrollmentService.cs
// ============================================================

using LifeTrack.PatientService.DTOs;
using LifeTrack.Shared.Wrappers;

namespace LifeTrack.PatientService.Services.Interfaces
{
    public interface IEnrollmentService
    {
        Task<ApiResponse<List<EnrollmentDto>>> GetAllAsync(EnrollmentFilterDto filter);
        Task<ApiResponse<EnrollmentDto>> GetByIdAsync(long id);
        Task<ApiResponse<EnrollmentDto>> EnrollAsync(EnrollPatientRequest req);
        Task<ApiResponse<bool>> RespondAsync(long enrollmentId, bool accept);
        Task<ApiResponse<bool>> UpdateStatusAsync(long id, UpdateEnrollmentStatusRequest req);
    }
}