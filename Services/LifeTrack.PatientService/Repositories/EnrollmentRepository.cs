// ============================================================
// PatientService.API / Repositories / EnrollmentRepository.cs
// ============================================================

using LifeTrack.PatientService.DTOs;
using LifeTrack.PatientService.Repositories.Interfaces;
using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.PatientService.Repositories
{
    public class EnrollmentRepository : IEnrollmentRepository
    {
        private readonly LifeTrackDbContext _db;

        public EnrollmentRepository(LifeTrackDbContext db) => _db = db;

        public async Task<List<EnrollmentDto>> GetAllAsync(EnrollmentFilterDto filter)
        {
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

            return await query
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
        }

        public async Task<EnrollmentDto?> GetByIdAsync(long id)
        {
            var e = await _db.Enrollments
                .Include(x => x.Patient)
                .Include(x => x.SiteProtocol)
                    .ThenInclude(sp => sp!.Site)
                .Include(x => x.SiteProtocol)
                    .ThenInclude(sp => sp!.Protocol)
                .FirstOrDefaultAsync(x => x.EnrollmentID == id);

            if (e == null) return null;

            return new EnrollmentDto
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
        }

        public async Task<EnrollmentDto> EnrollAsync(EnrollPatientRequest req)
        {
            var enrollment = new Enrollment
            {
                PatientID = req.PatientID,
                SiteProtocolID = req.SiteProtocolID,
                EnrollmentDate = DateTime.UtcNow,
                ConsentDate = req.ConsentDate,
                Status = "Active"
            };

            _db.Enrollments.Add(enrollment);
            await _db.SaveChangesAsync();

            return (await GetByIdAsync(enrollment.EnrollmentID))!;
        }

        public async Task<bool> UpdateStatusAsync(long id, UpdateEnrollmentStatusRequest req)
        {
            var e = await _db.Enrollments.FindAsync(id);
            if (e == null) return false;

            e.Status = req.Status;
            e.WithdrawalReason = req.WithdrawalReason;

            await _db.SaveChangesAsync();
            return true;
        }
    }
}