// ============================================================
// VisitService.API / Repositories / VisitRepository.cs
// ============================================================

using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using LifeTrack.VisitService.DTOs;
using LifeTrack.VisitService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.VisitService.Repositories
{
    public class VisitRepository : IVisitRepository
    {
        private readonly LifeTrackDbContext _db;

        public VisitRepository(LifeTrackDbContext db) => _db = db;

        public async Task<List<VisitDto>> GetAllAsync(VisitFilterDto filter)
        {
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

            return await query
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
        }

        public async Task<VisitDto?> GetByIdAsync(long id)
        {
            var v = await _db.Visits
                .Include(x => x.Enrollment)
                    .ThenInclude(e => e!.Patient)
                .Include(x => x.Enrollment)
                    .ThenInclude(e => e!.SiteProtocol)
                        .ThenInclude(sp => sp!.Protocol)
                .FirstOrDefaultAsync(x => x.VisitID == id);

            if (v == null) return null;

            return new VisitDto
            {
                VisitID = v.VisitID,
                EnrollmentID = v.EnrollmentID,
                PatientName = v.Enrollment?.Patient?.Name ?? "",
                ProtocolTitle = v.Enrollment?.SiteProtocol?.Protocol?.Title ?? "",
                VisitDate = v.VisitDate,
                Status = v.Status,
                Notes = v.Notes
            };
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

            return (await GetByIdAsync(visit.VisitID))!;
        }

        public async Task<bool> UpdateStatusAsync(long id, string status)
        {
            var v = await _db.Visits.FindAsync(id);
            if (v == null) return false;

            v.Status = status;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var v = await _db.Visits.FindAsync(id);
            if (v == null) return false;

            _db.Visits.Remove(v);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}