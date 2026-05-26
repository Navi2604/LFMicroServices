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

        //public async Task<bool> UpdateStatusAsync(long id, string status)
        //{
        //    var ae = await _db.AdverseEvents.FindAsync(id);
        //    if (ae == null) return false;

        //    ae.Status = status;
        //    await _db.SaveChangesAsync();
        //    return true;
        //}

        public async Task<bool> UpdateStatusAsync(long id, string status, string updaterRole)
        {
            // Load AE with Patient navigation so we can include name in notification
            var ae = await _db.AdverseEvents
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.EventID == id);

            if (ae == null) return false;

            ae.Status = status;

            // ── Fire notifications based on new status and who triggered it ──
            var notifications = BuildAENotifications(ae, status, updaterRole);
            if (notifications.Any())
            {
                // Determine which role to notify (2=CTM, 5=RO)
                var roleID = notifications[0].roleID;
                var message = notifications[0].message;
                var recipients = await _db.Users
                    .Where(u => u.RoleID == roleID && u.IsActive)
                    .Select(u => u.UserID)
                    .ToListAsync();

                foreach (var uid in recipients)
                {
                    _db.Notifications.Add(new LifeTrack.Shared.Models.Notification
                    {
                        UserID = uid,
                        Message = message,
                        Category = "AE",
                        Status = "Unread",
                        CreatedDate = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Returns the notification target role + message for an AE status change.
        /// Returns empty list if no notification is needed for this transition.
        /// </summary>
        private static List<(int roleID, string message)> BuildAENotifications(
            LifeTrack.Shared.Models.AdverseEvent ae,
            string newStatus,
            string updaterRole)
        {
            var id = ae.EventID;
            var name = ae.Patient?.Name ?? $"Patient #{ae.PatientID}";
            var severity = ae.Severity;

            // RoleID 5 = RegulatoryOfficer, RoleID 2 = ClinicalTrialManager
            return (newStatus, updaterRole) switch
            {
                ("Escalated", _) =>
                    [(5, $"⚠️ Adverse event AE-{id} has been escalated for regulatory review. " +
                 $"Patient: {name} | Severity: {severity}")],

                ("Resolved", "RegulatoryOfficer") =>
                    [(2, $"✅ AE-{id} has been resolved by the Regulatory Officer. " +
                 $"Patient: {name} | No further regulatory action required.")],

                ("Under Review", "RegulatoryOfficer") =>
                    [(2, $"↩️ AE-{id} has been returned by the Regulatory Officer for additional review. " +
                 $"Patient: {name} | Severity: {severity}")],

                _ => []
            };
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