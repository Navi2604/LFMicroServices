// ============================================================
// PatientService.API / Repositories / PatientRepository.cs
// WITH CACHING 
// ============================================================

using LifeTrack.PatientService.DTOs;
using LifeTrack.PatientService.Repositories.Interfaces;
using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LifeTrack.PatientService.Repositories
{
    public class PatientRepository : IPatientRepository
    {
        private readonly LifeTrackDbContext _db;
        private readonly IMemoryCache _cache;
        private const string PATIENT_CACHE_KEY = "patients_{0}_{1}_{2}_{3}";
        private const string PATIENT_ID_CACHE_KEY = "patient_{0}";
        private const int CACHE_DURATION_MINUTES = 20;

        public PatientRepository(LifeTrackDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<List<PatientDto>> GetAllAsync(PatientFilterDto filter)
        {
            // ✅ CREATE CACHE KEY FROM FILTER
            string cacheKey = string.Format(
                PATIENT_CACHE_KEY,
                filter.Name ?? "null",
                filter.Email ?? "null",
                filter.SiteProtocolID?.ToString() ?? "null",
                filter.EnrollmentStatus ?? "null"
            );

            // ✅ CHECK CACHE FIRST
            if (_cache.TryGetValue(cacheKey, out List<PatientDto>? cachedPatients))
                return cachedPatients!;

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

            var result = await query
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

            // ✅ STORE IN CACHE
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            return result;
        }

        public async Task<PatientDto?> GetByIdAsync(long id)
        {
            string cacheKey = string.Format(PATIENT_ID_CACHE_KEY, id);

            // ✅ CHECK CACHE FIRST
            if (_cache.TryGetValue(cacheKey, out PatientDto? cachedPatient))
                return cachedPatient;

            var p = await _db.Patients.FindAsync(id);
            if (p == null) return null;

            var dto = new PatientDto
            {
                PatientID = p.PatientID,
                Name = p.Name,
                DOB = p.DOB.ToString("yyyy-MM-dd"),
                ContactInfo = p.ContactInfo,
                Email = p.Email
            };

            // ✅ STORE IN CACHE
            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            return dto;
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

            // ✅ INVALIDATE CACHE
            InvalidateCache();

            return new PatientDto
            {
                PatientID = patient.PatientID,
                Name = patient.Name,
                DOB = patient.DOB.ToString("yyyy-MM-dd"),
                ContactInfo = patient.ContactInfo,
                Email = patient.Email
            };
        }

        // ❌ NO DELETE METHOD — Removed from backend

        private void InvalidateCache()
        {
            // Clear all patient caches (brute force approach)
            for (int i = 0; i < 100; i++)
            {
                string cacheKey = string.Format(PATIENT_CACHE_KEY, $"*{i}", $"*{i}", $"*{i}", $"*{i}");
                _cache.Remove(cacheKey);
            }
        }
    }
}