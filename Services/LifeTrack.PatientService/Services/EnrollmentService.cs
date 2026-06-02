// ============================================================
// PatientService.API / Services / EnrollmentService.cs
// ============================================================

using LifeTrack.PatientService.DTOs;
using LifeTrack.PatientService.Repositories.Interfaces;
using LifeTrack.PatientService.Services.Interfaces;
using LifeTrack.Shared.Helpers;
using LifeTrack.Shared.Wrappers;

namespace LifeTrack.PatientService.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IEnrollmentRepository _repo;
        private readonly AuditHttpClient _audit;

        public EnrollmentService(IEnrollmentRepository repo, AuditHttpClient audit)
        {
            _repo = repo;
            _audit = audit;
        }

        public async Task<ApiResponse<List<EnrollmentDto>>> GetAllAsync(EnrollmentFilterDto filter)
            => ApiResponse<List<EnrollmentDto>>.Ok(await _repo.GetAllAsync(filter));

        public async Task<ApiResponse<EnrollmentDto>> GetByIdAsync(long id)
        {
            var e = await _repo.GetByIdAsync(id);
            return e == null
                ? ApiResponse<EnrollmentDto>.Fail("Enrollment not found.")
                : ApiResponse<EnrollmentDto>.Ok(e);
        }

        public async Task<ApiResponse<EnrollmentDto>> EnrollAsync(EnrollPatientRequest req)
        {
            var enrollment = await _repo.EnrollAsync(req);

            _audit.Log("CREATE", "Enrollment", enrollment.EnrollmentID,
                $"Patient '{enrollment.PatientName}' enrolled in protocol '{enrollment.ProtocolTitle}' " +
                $"at site '{enrollment.SiteName}'.");

            return ApiResponse<EnrollmentDto>.Ok(enrollment, "Patient enrolled successfully.");
        }

        public async Task<ApiResponse<bool>> UpdateStatusAsync(long id, UpdateEnrollmentStatusRequest req)
        {
            var updated = await _repo.UpdateStatusAsync(id, req);

            if (updated)
                _audit.Log("UPDATE", "Enrollment", id,
                    $"Enrollment status updated to '{req.Status}'." +
                    (string.IsNullOrEmpty(req.WithdrawalReason)
                        ? ""
                        : $" Reason: '{req.WithdrawalReason}'."));

            return updated
                ? ApiResponse<bool>.Ok(true, "Enrollment status updated.")
                : ApiResponse<bool>.Fail("Enrollment not found.");
        }
    }
}