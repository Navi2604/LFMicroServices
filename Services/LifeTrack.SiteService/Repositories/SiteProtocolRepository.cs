// ============================================================
// SiteService.API / Repositories / SiteProtocolRepository.cs
// ============================================================

using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using LifeTrack.SiteService.DTOs;
using LifeTrack.SiteService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.SiteService.Repositories
{
    public class SiteProtocolRepository : ISiteProtocolRepository
    {
        private readonly LifeTrackDbContext _db;

        public SiteProtocolRepository(LifeTrackDbContext db) => _db = db;

        public async Task<List<SiteProtocolDto>> GetAllAsync(SiteProtocolFilterDto filter)
        {
            var query = _db.SiteProtocols
                .Include(sp => sp.Site)
                .Include(sp => sp.Protocol)
                .Include(sp => sp.Investigator)
                .AsQueryable();

            if (filter.SiteID.HasValue)
                query = query.Where(sp => sp.SiteID == filter.SiteID.Value);

            if (filter.ProtocolID.HasValue)
                query = query.Where(sp => sp.ProtocolID == filter.ProtocolID.Value);

            if (filter.InvestigatorID.HasValue)
                query = query.Where(sp => sp.InvestigatorID == filter.InvestigatorID.Value);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(sp => sp.Status == filter.Status);

            return await query
                .OrderByDescending(sp => sp.SiteProtocolID)
                .Select(sp => new SiteProtocolDto
                {
                    SiteProtocolID = sp.SiteProtocolID,
                    SiteID = sp.SiteID,
                    SiteName = sp.Site != null ? sp.Site.Name : "",
                    ProtocolID = sp.ProtocolID,
                    ProtocolTitle = sp.Protocol != null ? sp.Protocol.Title : "",
                    InvestigatorID = sp.InvestigatorID,
                    InvestigatorName = sp.Investigator != null ? sp.Investigator.Name : "",
                    Status = sp.Status,
                    ProtocolStatus = sp.Protocol != null ? sp.Protocol.Status : "",
                    StartDate = sp.Protocol != null ? sp.Protocol.StartDate : (DateTime?)null,
                    EndDate = sp.Protocol != null ? sp.Protocol.EndDate : (DateTime?)null,
                    InitiationDate = sp.InitiationDate
                })
                .ToListAsync();
        }

        public async Task<SiteProtocolDto?> GetByIdAsync(long id)
        {
            var sp = await _db.SiteProtocols
                .Include(x => x.Site)
                .Include(x => x.Protocol)
                .Include(x => x.Investigator)
                .FirstOrDefaultAsync(x => x.SiteProtocolID == id);

            if (sp == null) return null;

            return new SiteProtocolDto
            {
                SiteProtocolID = sp.SiteProtocolID,
                SiteID = sp.SiteID,
                SiteName = sp.Site?.Name ?? "",
                ProtocolID = sp.ProtocolID,
                ProtocolTitle = sp.Protocol?.Title ?? "",
                InvestigatorID = sp.InvestigatorID,
                InvestigatorName = sp.Investigator?.Name ?? "",
                Status = sp.Status,
                ProtocolStatus = sp.Protocol?.Status ?? "",
                StartDate = sp.Protocol?.StartDate,
                EndDate = sp.Protocol?.EndDate,
                InitiationDate = sp.InitiationDate
            };
        }

        public async Task<SiteProtocolDto> CreateAsync(CreateSiteProtocolRequest req)
        {
            // Block assignment if site is Inactive
            var site = await _db.Sites.FindAsync(req.SiteID);
            if (site == null)
                throw new InvalidOperationException("Site not found.");
            if (site.Status == "Inactive")
                throw new InvalidOperationException(
                    $"Cannot assign a protocol to '{site.Name}' because it is Inactive. Activate the site first.");

            // Block assignment if protocol is not Upcoming
            var protocol = await _db.Protocols.FindAsync(req.ProtocolID);
            if (protocol == null)
                throw new InvalidOperationException("Protocol not found.");
            if (protocol.Status != "Upcoming")
                throw new InvalidOperationException(
                    $"Cannot assign a site to '{protocol.Title}' because it is {protocol.Status}. Sites can only be added to Upcoming protocols.");

            var sp = new SiteProtocol
            {
                SiteID = req.SiteID,
                ProtocolID = req.ProtocolID,
                InvestigatorID = req.InvestigatorID,
                InitiationDate = req.InitiationDate,
                Status = req.Status
            };

            _db.SiteProtocols.Add(sp);
            await _db.SaveChangesAsync();

            // Auto-activate the investigator when first assigned to a site-protocol
            await SetInvestigatorActiveAsync(req.InvestigatorID, true);

            return (await GetByIdAsync(sp.SiteProtocolID))!;
        }

        public async Task<bool> UpdateStatusAsync(long id, string status)
        {
            var sp = await _db.SiteProtocols.FindAsync(id);
            if (sp == null) return false;

            sp.Status = status;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var sp = await _db.SiteProtocols
                .FirstOrDefaultAsync(x => x.SiteProtocolID == id);
            if (sp == null) return false;

            long investigatorId = sp.InvestigatorID;

            _db.SiteProtocols.Remove(sp);
            await _db.SaveChangesAsync();

            // Check if investigator still has any remaining site-protocols
            bool hasOtherAssignments = await _db.SiteProtocols
                .AnyAsync(x => x.InvestigatorID == investigatorId);

            // If no more assignments → auto-deactivate
            if (!hasOtherAssignments)
                await SetInvestigatorActiveAsync(investigatorId, false);

            return true;
        }

        // ── Set investigator IsActive directly via shared DbContext ──────────
        private async Task SetInvestigatorActiveAsync(long userId, bool active)
        {
            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserID == userId
                                       && u.Role != null
                                       && u.Role.RoleName == "Investigator");
            if (user == null) return;

            user.IsActive = active;
            await _db.SaveChangesAsync();
        }
    }
}