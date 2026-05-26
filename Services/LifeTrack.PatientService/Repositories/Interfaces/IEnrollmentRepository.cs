// ============================================================
// PatientService.API / Repositories / Interfaces / IEnrollmentRepository.cs
// NO DELETE — Enrollments managed via status updates
// ============================================================

using LifeTrack.PatientService.DTOs;

namespace LifeTrack.PatientService.Repositories.Interfaces
{
    public interface IEnrollmentRepository
    {
        Task<List<EnrollmentDto>> GetAllAsync(EnrollmentFilterDto filter);
        Task<EnrollmentDto?> GetByIdAsync(long id);
        Task<EnrollmentDto> EnrollAsync(EnrollPatientRequest req);
        Task<bool> RespondAsync(long enrollmentId, bool accept);
        Task<bool> UpdateStatusAsync(long id, UpdateEnrollmentStatusRequest req);
        // ❌ NO DELETE — Enrollments are immutable records
    }
}