// ============================================================
// SiteService.API / Repositories / SiteProtocolRepository.cs
// WITH CACHING 
// ============================================================

using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using LifeTrack.SiteService.DTOs;
using LifeTrack.SiteService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LifeTrack.SiteService.Repositories
{
    public class SiteProtocolRepository : ISiteProtocolRepository
    {
        private readonly LifeTrackDbContext _db;
        private readonly IMemoryCache _cache;
        private const string SITE_PROTOCOL_CACHE_KEY = "site_protocols_{0}_{1}_{2}_{3}";
        private const string SITE_PROTOCOL_ID_CACHE_KEY = "site_protocol_{0}";
        private const int CACHE_DURATION_MINUTES = 15;

        public SiteProtocolRepository(LifeTrackDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<List<SiteProtocolDto>> GetAllAsync(SiteProtocolFilterDto filter)
        {
            // ✅ CREATE CACHE KEY FROM FILTER
            string cacheKey = string.Format(
                SITE_PROTOCOL_CACHE_KEY,
                filter.SiteID?.ToString() ?? "null",
                filter.ProtocolID?.ToString() ?? "null",
                filter.InvestigatorID?.ToString() ?? "null",
                filter.Status ?? "null"
            );

            // ✅ CHECK CACHE FIRST
            if (_cache.TryGetValue(cacheKey, out List<SiteProtocolDto>? cachedSPs))
                return cachedSPs!;

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

            var result = await query
                .OrderByDescending(sp => sp.SiteProtocolID)
                .Select(sp => new SiteProtocolDto
                {
                    SiteProtocolID = sp.SiteProtocolID,
                    SiteID = sp.SiteID,
                    SiteName = sp.Site != null ? sp.Site.Name : "",
                    SiteLocation = sp.Site != null ? sp.Site.Location : "",
                    ProtocolID = sp.ProtocolID,
                    ProtocolTitle = sp.Protocol != null ? sp.Protocol.Title : "",
                    InvestigatorID = sp.InvestigatorID,
                    InvestigatorName = sp.Investigator != null ? sp.Investigator.Name : "",
                    InvestigatorEmail = sp.Investigator != null ? sp.Investigator.Email : "",
                    InvestigatorContact = sp.Investigator != null ? sp.Investigator.Phone : "",
                    Status = sp.Status,
                    ProtocolStatus = sp.Protocol != null ? sp.Protocol.Status : "",
                    StartDate = sp.Protocol != null ? sp.Protocol.StartDate : (DateTime?)null,
                    EndDate = sp.Protocol != null ? sp.Protocol.EndDate : (DateTime?)null,
                    InitiationDate = sp.InitiationDate
                })
                .ToListAsync();

            // ✅ STORE IN CACHE
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            return result;
        }

        public async Task<SiteProtocolDto?> GetByIdAsync(long id)
        {
            string cacheKey = string.Format(SITE_PROTOCOL_ID_CACHE_KEY, id);

            // ✅ CHECK CACHE FIRST
            if (_cache.TryGetValue(cacheKey, out SiteProtocolDto? cachedSP))
                return cachedSP;

            var sp = await _db.SiteProtocols
                .Include(x => x.Site)
                .Include(x => x.Protocol)
                .Include(x => x.Investigator)
                .FirstOrDefaultAsync(x => x.SiteProtocolID == id);

            if (sp == null) return null;

            var dto = new SiteProtocolDto
            {
                SiteProtocolID = sp.SiteProtocolID,
                SiteID = sp.SiteID,
                SiteName = sp.Site?.Name ?? "",
                SiteLocation = sp.Site?.Location ?? "",
                ProtocolID = sp.ProtocolID,
                ProtocolTitle = sp.Protocol?.Title ?? "",
                InvestigatorID = sp.InvestigatorID,
                InvestigatorName = sp.Investigator?.Name ?? "",
                InvestigatorEmail = sp.Investigator?.Email ?? "",
                InvestigatorContact = sp.Investigator?.Phone ?? "",
                Status = sp.Status,
                ProtocolStatus = sp.Protocol?.Status ?? "",
                StartDate = sp.Protocol?.StartDate,
                EndDate = sp.Protocol?.EndDate,
                InitiationDate = sp.InitiationDate
            };

            // ✅ STORE IN CACHE
            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            return dto;
        }

        public async Task<SiteProtocolDto> CreateAsync(CreateSiteProtocolRequest req)
        {
            var site = await _db.Sites.FindAsync(req.SiteID);
            if (site == null)
                throw new InvalidOperationException("Site not found.");
            if (site.Status == "Inactive")
                throw new InvalidOperationException(
                    $"Cannot assign a protocol to '{site.Name}' because it is Inactive. Activate the site first.");

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

            await SetInvestigatorActiveAsync(req.InvestigatorID, true);

            // ✅ INVALIDATE CACHE
            InvalidateSiteProtocolCache();

            return (await GetByIdAsync(sp.SiteProtocolID))!;
        }

        public async Task<bool> UpdateStatusAsync(long id, string status)
        {
            var sp = await _db.SiteProtocols.FindAsync(id);
            if (sp == null) return false;

            sp.Status = status;
            await _db.SaveChangesAsync();

            // ✅ INVALIDATE CACHE
            InvalidateSiteProtocolCache();

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

            bool hasOtherAssignments = await _db.SiteProtocols
                .AnyAsync(x => x.InvestigatorID == investigatorId);

            if (!hasOtherAssignments)
                await SetInvestigatorActiveAsync(investigatorId, false);

            // ✅ INVALIDATE CACHE
            InvalidateSiteProtocolCache();

            return true;
        }

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

        private void InvalidateSiteProtocolCache()
        {
            // Clear all site protocol caches (brute force)
            for (int i = 0; i < 100; i++)
            {
                string cacheKey = string.Format(SITE_PROTOCOL_CACHE_KEY, $"*{i}", $"*{i}", $"*{i}", $"*{i}");
                _cache.Remove(cacheKey);
            }
        }
    }
}