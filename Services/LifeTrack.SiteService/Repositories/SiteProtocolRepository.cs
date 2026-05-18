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
                    InitiationDate = sp.InitiationDate,
                    Status = sp.Status
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
                InitiationDate = sp.InitiationDate,
                Status = sp.Status
            };
        }

        public async Task<SiteProtocolDto> CreateAsync(CreateSiteProtocolRequest req)
        {
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
            var sp = await _db.SiteProtocols.FindAsync(id);
            if (sp == null) return false;

            _db.SiteProtocols.Remove(sp);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}