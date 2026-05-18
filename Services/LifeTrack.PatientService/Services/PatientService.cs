// ============================================================
// PatientService.API / Services / PatientService.cs
// ============================================================

using LifeTrack.PatientService.DTOs;
using LifeTrack.PatientService.Repositories.Interfaces;
using LifeTrack.PatientService.Services.Interfaces;
using LifeTrack.Shared.Helpers;
using LifeTrack.Shared.Wrappers;

namespace LifeTrack.PatientService.Services
{
    public class PatientService : IPatientService
    {
        private readonly IPatientRepository _repo;
        private readonly AuditHttpClient _audit;

        public PatientService(IPatientRepository repo, AuditHttpClient audit)
        {
            _repo = repo;
            _audit = audit;
        }

        public async Task<ApiResponse<List<PatientDto>>> GetAllAsync(PatientFilterDto filter)
            => ApiResponse<List<PatientDto>>.Ok(await _repo.GetAllAsync(filter));

        public async Task<ApiResponse<PatientDto>> GetByIdAsync(long id)
        {
            var p = await _repo.GetByIdAsync(id);
            return p == null
                ? ApiResponse<PatientDto>.Fail("Patient not found.")
                : ApiResponse<PatientDto>.Ok(p);
        }

        public async Task<ApiResponse<PatientDto>> CreateAsync(CreatePatientRequest req)
        {
            var patient = await _repo.CreateAsync(req);

            _audit.Log("CREATE", "Patient", patient.PatientID,
                $"Patient '{patient.Name}' ({patient.Email}) registered.");

            return ApiResponse<PatientDto>.Ok(patient, "Patient created successfully.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            var existing = await _repo.GetByIdAsync(id);
            var deleted = await _repo.DeleteAsync(id);

            if (deleted)
                _audit.Log("DELETE", "Patient", id,
                    $"Patient '{existing?.Name}' deleted.");

            return deleted
                ? ApiResponse<bool>.Ok(true, "Patient deleted successfully.")
                : ApiResponse<bool>.Fail("Patient not found.");
        }
    }
}