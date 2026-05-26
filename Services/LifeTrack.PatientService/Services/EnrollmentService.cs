// ============================================================
// PatientService.API / Services / EnrollmentService.cs
// WITH CACHING — No changes needed (caching at repo level)
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

        // ✅ Caching is handled at repository level
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
                $"Enrollment invitation sent to patient '{enrollment.PatientName}' " +
                $"for protocol '{enrollment.ProtocolTitle}' at '{enrollment.SiteName}'. Status: Pending.");

            return ApiResponse<EnrollmentDto>.Ok(enrollment,
                "Enrollment invitation sent. Awaiting patient consent.");
        }

        public async Task<ApiResponse<bool>> RespondAsync(long enrollmentId, bool accept)
        {
            var updated = await _repo.RespondAsync(enrollmentId, accept);
            if (!updated)
                return ApiResponse<bool>.Fail("Enrollment not found or already responded.");

            var action = accept ? "accepted" : "declined";
            _audit.Log("UPDATE", "Enrollment", enrollmentId,
                $"Patient {action} enrollment invitation (ID: {enrollmentId}).");

            return ApiResponse<bool>.Ok(true,
                accept ? "Enrollment accepted. You are now active in this protocol."
                       : "Enrollment declined.");
        }

        public async Task<ApiResponse<bool>> UpdateStatusAsync(long id, UpdateEnrollmentStatusRequest req)
        {
            var updated = await _repo.UpdateStatusAsync(id, req);
            if (!updated)
                return ApiResponse<bool>.Fail("Enrollment not found.");

            _audit.Log("UPDATE", "Enrollment", id,
                $"Enrollment status updated to '{req.Status}'.");

            return ApiResponse<bool>.Ok(true, "Enrollment status updated.");
        }
    }
}