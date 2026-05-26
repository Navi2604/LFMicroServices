// ============================================================
// SiteService.API / Repositories / DeviationRepository.cs
// ============================================================

using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using LifeTrack.SiteService.DTOs;
using LifeTrack.SiteService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.SiteService.Repositories
{
    public class DeviationRepository : IDeviationRepository
    {
        private readonly LifeTrackDbContext _db;

        public DeviationRepository(LifeTrackDbContext db) => _db = db;

        public async Task<List<DeviationDto>> GetAllAsync(DeviationFilterDto filter)
        {
            var query = _db.Deviations.AsQueryable();

            if (filter.SiteProtocolID.HasValue)
                query = query.Where(d => d.SiteProtocolID == filter.SiteProtocolID.Value);

            if (!string.IsNullOrEmpty(filter.Severity))
                query = query.Where(d => d.Severity == filter.Severity);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(d => d.Status == filter.Status);

            return await query
                .OrderByDescending(d => d.DeviationID)
                .Select(d => new DeviationDto
                {
                    DeviationID = d.DeviationID,
                    SiteProtocolID = d.SiteProtocolID,
                    Description = d.Description,
                    Severity = d.Severity,
                    Status = d.Status
                })
                .ToListAsync();
        }

        public async Task<DeviationDto?> GetByIdAsync(long id)
        {
            var d = await _db.Deviations.FindAsync(id);
            if (d == null) return null;

            return new DeviationDto
            {
                DeviationID = d.DeviationID,
                SiteProtocolID = d.SiteProtocolID,
                Description = d.Description,
                Severity = d.Severity,
                Status = d.Status
            };
        }

        public async Task<DeviationDto> CreateAsync(CreateDeviationRequest req)
        {
            var d = new Deviation
            {
                SiteProtocolID = req.SiteProtocolID,
                Description = req.Description,
                Severity = req.Severity,
                Status = "Open"
            };

            _db.Deviations.Add(d);
            await _db.SaveChangesAsync();

            return new DeviationDto
            {
                DeviationID = d.DeviationID,
                SiteProtocolID = d.SiteProtocolID,
                Description = d.Description,
                Severity = d.Severity,
                Status = d.Status
            };
        }

        public async Task<bool> UpdateStatusAsync(long id, string status, string updaterRole)
        {
            var d = await _db.Deviations
                .Include(dev => dev.SiteProtocol)
                .FirstOrDefaultAsync(dev => dev.DeviationID == id);

            if (d == null) return false;

            d.Status = status;

            // ── Notify CTMs when RO makes a status change ──
            if (updaterRole == "RegulatoryOfficer")
            {
                var siteName = d.SiteProtocol?.Site?.Name ?? $"SiteProtocol #{d.SiteProtocolID}";
                var message = $"📋 Deviation DEV-{id} reviewed by Regulatory Officer. " +
                               $"Status: {status} | Severity: {d.Severity} | Site: {siteName}";

                // RoleID 2 = ClinicalTrialManager
                var recipients = await _db.Users
                    .Where(u => u.RoleID == 2 && u.IsActive)
                    .Select(u => u.UserID)
                    .ToListAsync();

                foreach (var uid in recipients)
                {
                    _db.Notifications.Add(new LifeTrack.Shared.Models.Notification
                    {
                        UserID = uid,
                        Message = message,
                        Category = "Compliance",
                        Status = "Unread",
                        CreatedDate = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var d = await _db.Deviations.FindAsync(id);
            if (d == null) return false;

            _db.Deviations.Remove(d);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}