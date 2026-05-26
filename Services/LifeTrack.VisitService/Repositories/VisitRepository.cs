// ============================================================
// VisitService.API / Repositories / VisitRepository.cs
// WITH CACHING 
// ============================================================

using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using LifeTrack.VisitService.DTOs;
using LifeTrack.VisitService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LifeTrack.VisitService.Repositories
{
    public class VisitRepository : IVisitRepository
    {
        private readonly LifeTrackDbContext _db;
        private readonly IMemoryCache _cache;
        private const string VISIT_CACHE_KEY = "visits_{0}_{1}_{2}_{3}";
        private const string VISIT_ID_CACHE_KEY = "visit_{0}";
        private const int CACHE_DURATION_MINUTES = 10;

        public VisitRepository(LifeTrackDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<List<VisitDto>> GetAllAsync(VisitFilterDto filter)
        {
            // ✅ CREATE CACHE KEY FROM FILTER
            string cacheKey = string.Format(
                VISIT_CACHE_KEY,
                filter.EnrollmentID?.ToString() ?? "null",
                filter.Status ?? "null",
                filter.FromDate?.ToString("yyyy-MM-dd") ?? "null",
                filter.ToDate?.ToString("yyyy-MM-dd") ?? "null"
            );

            // ✅ CHECK CACHE FIRST
            if (_cache.TryGetValue(cacheKey, out List<VisitDto>? cachedVisits))
                return cachedVisits!;

            var query = _db.Visits
                .Include(v => v.Enrollment)
                    .ThenInclude(e => e!.Patient)
                .Include(v => v.Enrollment)
                    .ThenInclude(e => e!.SiteProtocol)
                        .ThenInclude(sp => sp!.Protocol)
                .AsQueryable();

            if (filter.EnrollmentID.HasValue)
                query = query.Where(v => v.EnrollmentID == filter.EnrollmentID.Value);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(v => v.Status == filter.Status);

            if (filter.FromDate.HasValue)
                query = query.Where(v => v.VisitDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(v => v.VisitDate <= filter.ToDate.Value);

            var result = await query
                .OrderByDescending(v => v.VisitDate)
                .Select(v => new VisitDto
                {
                    VisitID = v.VisitID,
                    EnrollmentID = v.EnrollmentID,
                    PatientName = v.Enrollment != null && v.Enrollment.Patient != null
                                        ? v.Enrollment.Patient.Name : "",
                    ProtocolTitle = v.Enrollment != null
                                    && v.Enrollment.SiteProtocol != null
                                    && v.Enrollment.SiteProtocol.Protocol != null
                                        ? v.Enrollment.SiteProtocol.Protocol.Title : "",
                    VisitDate = v.VisitDate,
                    Status = v.Status,
                    Notes = v.Notes
                })
                .ToListAsync();

            // ✅ STORE IN CACHE
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            return result;
        }

        public async Task<VisitDto?> GetByIdAsync(long id)
        {
            string cacheKey = string.Format(VISIT_ID_CACHE_KEY, id);

            // ✅ CHECK CACHE FIRST
            if (_cache.TryGetValue(cacheKey, out VisitDto? cachedVisit))
                return cachedVisit;

            var v = await _db.Visits
                .Include(x => x.Enrollment)
                    .ThenInclude(e => e!.Patient)
                .Include(x => x.Enrollment)
                    .ThenInclude(e => e!.SiteProtocol)
                        .ThenInclude(sp => sp!.Protocol)
                .FirstOrDefaultAsync(x => x.VisitID == id);

            if (v == null) return null;

            var dto = new VisitDto
            {
                VisitID = v.VisitID,
                EnrollmentID = v.EnrollmentID,
                PatientName = v.Enrollment?.Patient?.Name ?? "",
                ProtocolTitle = v.Enrollment?.SiteProtocol?.Protocol?.Title ?? "",
                VisitDate = v.VisitDate,
                Status = v.Status,
                Notes = v.Notes
            };

            // ✅ STORE IN CACHE
            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            return dto;
        }

        public async Task<VisitDto> CreateAsync(CreateVisitRequest req)
        {
            var visit = new Visit
            {
                EnrollmentID = req.EnrollmentID,
                VisitDate = req.VisitDate,
                Status = req.Status,
                Notes = req.Notes
            };

            _db.Visits.Add(visit);
            await _db.SaveChangesAsync();

            // ✅ INVALIDATE CACHE
            InvalidateVisitCache();

            return (await GetByIdAsync(visit.VisitID))!;
        }

        public async Task<bool> UpdateStatusAsync(long id, string status)
        {
            var v = await _db.Visits.FindAsync(id);
            if (v == null) return false;

            v.Status = status;
            await _db.SaveChangesAsync();

            // ✅ INVALIDATE CACHE
            InvalidateVisitCache();

            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var v = await _db.Visits.FindAsync(id);
            if (v == null) return false;

            // ✅ Only allow deletion of Scheduled visits
            if (v.Status != "Scheduled")
                return false;

            _db.Visits.Remove(v);
            await _db.SaveChangesAsync();

            // ✅ INVALIDATE CACHE
            InvalidateVisitCache();

            return true;
        }

        private void InvalidateVisitCache()
        {
            // Clear all visit caches (brute force)
            for (int i = 0; i < 100; i++)
            {
                string cacheKey = string.Format(VISIT_CACHE_KEY, $"*{i}", $"*{i}", $"*{i}", $"*{i}");
                _cache.Remove(cacheKey);
            }
        }
    }
}