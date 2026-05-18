// ============================================================
// PatientService.API / Repositories / PatientRepository.cs
// ============================================================

using LifeTrack.PatientService.DTOs;
using LifeTrack.PatientService.Repositories.Interfaces;
using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using Microsoft.EntityFrameworkCore;


namespace LifeTrack.PatientService.Repositories
{
    public class PatientRepository : IPatientRepository
    {
        private readonly LifeTrackDbContext _db;

        public PatientRepository(LifeTrackDbContext db) => _db = db;

        public async Task<List<PatientDto>> GetAllAsync(PatientFilterDto filter)
        {
            var query = _db.Patients.AsQueryable();

            if (!string.IsNullOrEmpty(filter.Name))
                query = query.Where(p => p.Name.Contains(filter.Name));

            if (!string.IsNullOrEmpty(filter.Email))
                query = query.Where(p => p.Email.Contains(filter.Email));

            if (filter.SiteProtocolID.HasValue)
                query = query.Where(p =>
                    p.Enrollments.Any(e => e.SiteProtocolID == filter.SiteProtocolID.Value));

            if (!string.IsNullOrEmpty(filter.EnrollmentStatus))
                query = query.Where(p =>
                    p.Enrollments.Any(e => e.Status == filter.EnrollmentStatus));

            return await query
                .OrderByDescending(p => p.PatientID)
                .Select(p => new PatientDto
                {
                    PatientID = p.PatientID,
                    Name = p.Name,
                    DOB = p.DOB.ToString("yyyy-MM-dd"),
                    ContactInfo = p.ContactInfo,
                    Email = p.Email
                })
                .ToListAsync();
        }

        public async Task<PatientDto?> GetByIdAsync(long id)
        {
            var p = await _db.Patients.FindAsync(id);
            if (p == null) return null;

            return new PatientDto
            {
                PatientID = p.PatientID,
                Name = p.Name,
                DOB = p.DOB.ToString("yyyy-MM-dd"),
                ContactInfo = p.ContactInfo,
                Email = p.Email
            };
        }

        public async Task<PatientDto> CreateAsync(CreatePatientRequest req)
        {
            var patient = new Patient
            {
                Name = req.Name,
                Email = req.Email,
                DOB = req.DOB,
                ContactInfo = req.ContactInfo,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
            };

            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();

            return new PatientDto
            {
                PatientID = patient.PatientID,
                Name = patient.Name,
                DOB = patient.DOB.ToString("yyyy-MM-dd"),
                ContactInfo = patient.ContactInfo,
                Email = patient.Email
            };
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var p = await _db.Patients.FindAsync(id);
            if (p == null) return false;

            _db.Patients.Remove(p);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}