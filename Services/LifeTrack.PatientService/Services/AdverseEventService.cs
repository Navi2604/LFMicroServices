// ============================================================
// PatientService.API / Services / AdverseEventService.cs
// ============================================================

using LifeTrack.PatientService.DTOs;
using LifeTrack.PatientService.Repositories.Interfaces;
using LifeTrack.PatientService.Services.Interfaces;
using LifeTrack.Shared.Helpers;
using LifeTrack.Shared.Wrappers;

namespace LifeTrack.PatientService.Services
{
    public class AdverseEventService : IAdverseEventService
    {
        private readonly IAdverseEventRepository _repo;
        private readonly AuditHttpClient _audit;

        public AdverseEventService(IAdverseEventRepository repo, AuditHttpClient audit)
        {
            _repo = repo;
            _audit = audit;
        }

        public async Task<ApiResponse<List<AdverseEventDto>>> GetAllAsync(AdverseEventFilterDto filter)
            => ApiResponse<List<AdverseEventDto>>.Ok(await _repo.GetAllAsync(filter));

        public async Task<ApiResponse<AdverseEventDto>> CreateAsync(CreateAdverseEventRequest req)
        {
            var ae = await _repo.CreateAsync(req);

            _audit.Log("CREATE", "AdverseEvent", ae.EventID,
                $"Adverse event reported for Patient ID {ae.PatientID}. " +
                $"Severity: '{ae.Severity}'. Description: '{ae.Description}'.");

            return ApiResponse<AdverseEventDto>.Ok(ae, "Adverse event reported successfully.");
        }

        public async Task<ApiResponse<bool>> UpdateStatusAsync(long id, string status, string updaterRole)
        {
            var updated = await _repo.UpdateStatusAsync(id, status, updaterRole);

            if (updated)
                _audit.Log("UPDATE", "AdverseEvent", id,
                    $"Adverse event status updated to '{status}' by role '{updaterRole}'.");

            return updated
                ? ApiResponse<bool>.Ok(true, "Status updated successfully.")
                : ApiResponse<bool>.Fail("Adverse event not found.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            var deleted = await _repo.DeleteAsync(id);

            if (deleted)
                _audit.Log("DELETE", "AdverseEvent", id,
                    $"Adverse event ID {id} deleted.");

            return deleted
                ? ApiResponse<bool>.Ok(true, "Adverse event deleted.")
                : ApiResponse<bool>.Fail("Adverse event not found.");
        }
    }
}