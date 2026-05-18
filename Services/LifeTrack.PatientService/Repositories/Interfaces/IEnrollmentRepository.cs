// ============================================================
// PatientService.API / Repositories / Interfaces / IEnrollmentRepository.cs
// ============================================================

using LifeTrack.PatientService.DTOs;

namespace LifeTrack.PatientService.Repositories.Interfaces
{
    public interface IEnrollmentRepository
    {
        Task<List<EnrollmentDto>> GetAllAsync(EnrollmentFilterDto filter);
        Task<EnrollmentDto?> GetByIdAsync(long id);
        Task<EnrollmentDto> EnrollAsync(EnrollPatientRequest req);
        Task<bool> UpdateStatusAsync(long id, UpdateEnrollmentStatusRequest req);
    }
}