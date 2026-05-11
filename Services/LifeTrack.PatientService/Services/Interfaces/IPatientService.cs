using LifeTrack.PatientService.DTOs;
using LifeTrack.Shared.Wrappers;

namespace LifeTrack.PatientService.Services.Interfaces
{
    public interface IPatientService
    {
        Task<ApiResponse<List<PatientDto>>> GetAllAsync();
        Task<ApiResponse<PatientDto>> GetByIdAsync(long id);
        Task<ApiResponse<PatientDto>> EnrollAsync(
            EnrollPatientRequest req, long investigatorId);
        Task<ApiResponse<PatientDto>> UpdateStatusAsync(
            long id, UpdateStatusRequest req);
        Task<ApiResponse<bool>> DeleteAsync(long id);
    }
}