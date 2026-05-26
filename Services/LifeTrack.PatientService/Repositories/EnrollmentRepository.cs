// ============================================================
// PatientService.API / Repositories / EnrollmentRepository.cs
// WITH CACHING — Updated
// ============================================================

using LifeTrack.PatientService.DTOs;
using LifeTrack.PatientService.Repositories.Interfaces;
using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LifeTrack.PatientService.Repositories
{
    public class EnrollmentRepository : IEnrollmentRepository
    {
        private readonly LifeTrackDbContext _db;
        private readonly IMemoryCache _cache;
        private const string ENROLLMENT_CACHE_KEY = "enrollments_{0}_{1}_{2}";
        private const string ENROLLMENT_ID_CACHE_KEY = "enrollment_{0}";
        private const int CACHE_DURATION_MINUTES = 10;

        public EnrollmentRepository(LifeTrackDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<List<EnrollmentDto>> GetAllAsync(EnrollmentFilterDto filter)
        {
            // ✅ CREATE CACHE KEY FROM FILTER
            string cacheKey = string.Format(
                ENROLLMENT_CACHE_KEY,
                filter.PatientID?.ToString() ?? "null",
                filter.SiteProtocolID?.ToString() ?? "null",
                filter.Status ?? "null"
            );

            // ✅ CHECK CACHE FIRST
            if (_cache.TryGetValue(cacheKey, out List<EnrollmentDto>? cachedEnrollments))
                return cachedEnrollments!;

            var query = _db.Enrollments
                .Include(e => e.Patient)
                .Include(e => e.SiteProtocol)
                    .ThenInclude(sp => sp!.Site)
                .Include(e => e.SiteProtocol)
                    .ThenInclude(sp => sp!.Protocol)
                .AsQueryable();

            if (filter.PatientID.HasValue)
                query = query.Where(e => e.PatientID == filter.PatientID.Value);

            if (filter.SiteProtocolID.HasValue)
                query = query.Where(e => e.SiteProtocolID == filter.SiteProtocolID.Value);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(e => e.Status == filter.Status);

            var result = await query
                .OrderByDescending(e => e.EnrollmentID)
                .Select(e => new EnrollmentDto
                {
                    EnrollmentID = e.EnrollmentID,
                    PatientID = e.PatientID,
                    PatientName = e.Patient != null ? e.Patient.Name : "",
                    SiteProtocolID = e.SiteProtocolID,
                    SiteName = e.SiteProtocol != null && e.SiteProtocol.Site != null
                                           ? e.SiteProtocol.Site.Name : "",
                    ProtocolTitle = e.SiteProtocol != null && e.SiteProtocol.Protocol != null
                                           ? e.SiteProtocol.Protocol.Title : "",
                    Status = e.Status,
                    EnrollmentDate = e.EnrollmentDate,
                    WithdrawalReason = e.WithdrawalReason
                })
                .ToListAsync();

            // ✅ STORE IN CACHE
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            return result;
        }

        public async Task<EnrollmentDto?> GetByIdAsync(long id)
        {
            string cacheKey = string.Format(ENROLLMENT_ID_CACHE_KEY, id);

            // ✅ CHECK CACHE FIRST
            if (_cache.TryGetValue(cacheKey, out EnrollmentDto? cachedEnrollment))
                return cachedEnrollment;

            var e = await _db.Enrollments
                .Include(x => x.Patient)
                .Include(x => x.SiteProtocol)
                    .ThenInclude(sp => sp!.Site)
                .Include(x => x.SiteProtocol)
                    .ThenInclude(sp => sp!.Protocol)
                .FirstOrDefaultAsync(x => x.EnrollmentID == id);

            if (e == null) return null;

            var dto = new EnrollmentDto
            {
                EnrollmentID = e.EnrollmentID,
                PatientID = e.PatientID,
                PatientName = e.Patient?.Name ?? "",
                SiteProtocolID = e.SiteProtocolID,
                SiteName = e.SiteProtocol?.Site?.Name ?? "",
                ProtocolTitle = e.SiteProtocol?.Protocol?.Title ?? "",
                Status = e.Status,
                EnrollmentDate = e.EnrollmentDate,
                WithdrawalReason = e.WithdrawalReason
            };

            // ✅ STORE IN CACHE
            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            return dto;
        }

        public async Task<EnrollmentDto> EnrollAsync(EnrollPatientRequest req)
        {
            var enrollment = new Enrollment
            {
                PatientID = req.PatientID,
                SiteProtocolID = req.SiteProtocolID,
                EnrollmentDate = DateTime.UtcNow,
                ConsentDate = req.ConsentDate,
                Status = "Pending"
            };

            _db.Enrollments.Add(enrollment);
            await _db.SaveChangesAsync();

            // ✅ INVALIDATE CACHE
            InvalidateEnrollmentCache();

            return (await GetByIdAsync(enrollment.EnrollmentID))!;
        }

        public async Task<bool> RespondAsync(long enrollmentId, bool accept)
        {
            var e = await _db.Enrollments.FindAsync(enrollmentId);
            if (e == null) return false;

            if (e.Status == "Pending")
            {
                e.Status = accept ? "Active" : "Declined";
                e.ConsentDate = accept ? DateTime.UtcNow : null;
            }
            else if (e.Status == "PendingWithdrawal")
            {
                e.Status = accept ? "Withdrawn" : "Active";
            }
            else return false;

            await _db.SaveChangesAsync();

            // ✅ INVALIDATE CACHE
            InvalidateEnrollmentCache();

            return true;
        }

        public async Task<bool> UpdateStatusAsync(long id, UpdateEnrollmentStatusRequest req)
        {
            var e = await _db.Enrollments.FindAsync(id);
            if (e == null) return false;

            e.Status = req.Status;
            e.WithdrawalReason = req.WithdrawalReason;

            await _db.SaveChangesAsync();

            // ✅ INVALIDATE CACHE
            InvalidateEnrollmentCache();

            return true;
        }

        private void InvalidateEnrollmentCache()
        {
            // Clear all enrollment caches (brute force)
            for (int i = 0; i < 100; i++)
            {
                string cacheKey = string.Format(ENROLLMENT_CACHE_KEY, $"*{i}", $"*{i}", $"*{i}");
                _cache.Remove(cacheKey);
            }
        }
    }
}