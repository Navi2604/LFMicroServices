// ============================================================
// PatientService.API / Repositories / AdverseEventRepository.cs
// ============================================================

using LifeTrack.PatientService.DTOs;
using LifeTrack.PatientService.Repositories.Interfaces;
using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.PatientService.Repositories
{
    public class AdverseEventRepository : IAdverseEventRepository
    {
        private readonly LifeTrackDbContext _db;

        public AdverseEventRepository(LifeTrackDbContext db) => _db = db;

        public async Task<List<AdverseEventDto>> GetAllAsync(AdverseEventFilterDto filter)
        {
            var query = _db.AdverseEvents
                           .Include(ae => ae.Patient)
                           .AsQueryable();

            if (filter.PatientID.HasValue)
                query = query.Where(ae => ae.PatientID == filter.PatientID.Value);

            if (filter.ProtocolID.HasValue)
                query = query.Where(ae => ae.ProtocolID == filter.ProtocolID.Value);

            if (!string.IsNullOrEmpty(filter.Severity))
                query = query.Where(ae => ae.Severity == filter.Severity);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(ae => ae.Status == filter.Status);

            return await query
                .OrderByDescending(ae => ae.ReportedDate)
                .Select(ae => new AdverseEventDto
                {
                    EventID = ae.EventID,
                    PatientID = ae.PatientID,
                    PatientName = ae.Patient != null ? ae.Patient.Name : "",
                    ProtocolID = ae.ProtocolID,
                    Description = ae.Description,
                    Severity = ae.Severity,
                    Status = ae.Status,
                    ReportedDate = ae.ReportedDate
                })
                .ToListAsync();
        }

        public async Task<AdverseEventDto> CreateAsync(CreateAdverseEventRequest req)
        {
            var ae = new AdverseEvent
            {
                PatientID = req.PatientID,
                ProtocolID = req.ProtocolID,
                Description = req.Description,
                Severity = req.Severity,
                Status = "Open",
                ReportedDate = DateTime.UtcNow
            };

            _db.AdverseEvents.Add(ae);
            await _db.SaveChangesAsync();

            return new AdverseEventDto
            {
                EventID = ae.EventID,
                PatientID = ae.PatientID,
                ProtocolID = ae.ProtocolID,
                Description = ae.Description,
                Severity = ae.Severity,
                Status = ae.Status,
                ReportedDate = ae.ReportedDate
            };
        }

        public async Task<bool> UpdateStatusAsync(long id, string status)
        {
            var ae = await _db.AdverseEvents.FindAsync(id);
            if (ae == null) return false;

            ae.Status = status;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var ae = await _db.AdverseEvents.FindAsync(id);
            if (ae == null) return false;

            _db.AdverseEvents.Remove(ae);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}