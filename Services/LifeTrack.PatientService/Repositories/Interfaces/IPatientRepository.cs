// ============================================================
// PatientService.API / Repositories / Interfaces / IPatientRepository.cs
// DELETE METHOD REMOVED — Updated
// ============================================================

using LifeTrack.PatientService.DTOs;

namespace LifeTrack.PatientService.Repositories.Interfaces
{
    public interface IPatientRepository
    {
        Task<List<PatientDto>> GetAllAsync(PatientFilterDto filter);
        Task<PatientDto?> GetByIdAsync(long id);
        Task<PatientDto> CreateAsync(CreatePatientRequest req);
        // ❌ DELETE REMOVED
    }
}