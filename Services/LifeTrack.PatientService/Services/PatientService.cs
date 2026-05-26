// ============================================================
// PatientService.API / Services / PatientService.cs
// DELETE METHOD REMOVED — Updated
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

        // ❌ DELETE REMOVED — Patients are deactivated via enrollment status, not deleted
    }
}