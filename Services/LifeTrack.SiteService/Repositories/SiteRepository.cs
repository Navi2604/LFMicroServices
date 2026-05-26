// ============================================================
// SiteService.API / Repositories / SiteRepository.cs
// WITH CACHING AND EMAIL/CONTACT FIELDS
// ============================================================

using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using LifeTrack.SiteService.DTOs;
using LifeTrack.SiteService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LifeTrack.SiteService.Repositories
{
    public class SiteRepository : ISiteRepository
    {
        private readonly LifeTrackDbContext _db;
        private readonly IMemoryCache _cache;
        private const string SITE_CACHE_KEY = "sites_{0}_{1}_{2}";
        private const string SITE_ID_CACHE_KEY = "site_{0}";
        private const int CACHE_DURATION_MINUTES = 20;

        public SiteRepository(LifeTrackDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        // ── Normalise old status values to Active / Inactive ──────────────
        private static string NormaliseStatus(string status) => status switch
        {
            "Ongoing" => "Active",
            "Upcoming" => "Active",
            "Closed" => "Inactive",
            _ => status   // Already "Active" or "Inactive"
        };

        public async Task<List<SiteDto>> GetAllAsync(SiteFilterDto filter)
        {
            // ✅ CREATE CACHE KEY FROM FILTER
            string cacheKey = string.Format(
                SITE_CACHE_KEY,
                filter.Name ?? "null",
                filter.Location ?? "null",
                filter.Status ?? "null"
            );

            // ✅ CHECK CACHE FIRST
            if (_cache.TryGetValue(cacheKey, out List<SiteDto>? cachedSites))
                return cachedSites!;

            var query = _db.Sites.AsQueryable();

            if (!string.IsNullOrEmpty(filter.Name))
                query = query.Where(s => s.Name.Contains(filter.Name));

            if (!string.IsNullOrEmpty(filter.Location))
                query = query.Where(s => s.Location.Contains(filter.Location));

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(s => s.Status == filter.Status);

            var sites = await query
                .OrderByDescending(s => s.SiteID)
                .ToListAsync();

            var result = sites.Select(s => new SiteDto
            {
                SiteID = s.SiteID,
                Name = s.Name,
                Location = s.Location,
                Email = s.Email,
                Contact = s.Contact,
                Status = NormaliseStatus(s.Status)
            }).ToList();

            // ✅ STORE IN CACHE
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            return result;
        }

        public async Task<SiteDto?> GetByIdAsync(long id)
        {
            string cacheKey = string.Format(SITE_ID_CACHE_KEY, id);

            // ✅ CHECK CACHE FIRST
            if (_cache.TryGetValue(cacheKey, out SiteDto? cachedSite))
                return cachedSite;

            var s = await _db.Sites.FindAsync(id);
            if (s == null) return null;

            var dto = new SiteDto
            {
                SiteID = s.SiteID,
                Name = s.Name,
                Location = s.Location,
                Email = s.Email,
                Contact = s.Contact,
                Status = NormaliseStatus(s.Status)
            };

            // ✅ STORE IN CACHE
            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            return dto;
        }

        public async Task<SiteDto> CreateAsync(CreateSiteRequest req)
        {
            var site = new Site
            {
                Name = req.Name,
                Location = req.Location,
                Email = req.Email,
                Contact = req.Contact,
                Status = NormaliseStatus(req.Status)
            };

            _db.Sites.Add(site);
            await _db.SaveChangesAsync();

            // ✅ INVALIDATE CACHE
            InvalidateSiteCache();

            return new SiteDto
            {
                SiteID = site.SiteID,
                Name = site.Name,
                Location = site.Location,
                Email = site.Email,
                Contact = site.Contact,
                Status = site.Status
            };
        }

        public async Task<SiteDto?> UpdateAsync(long id, UpdateSiteRequest req)
        {
            var site = await _db.Sites.FindAsync(id);
            if (site == null) return null;

            site.Name = req.Name;
            site.Location = req.Location;
            site.Email = req.Email;
            site.Contact = req.Contact;
            site.Status = NormaliseStatus(req.Status);

            await _db.SaveChangesAsync();

            // ✅ INVALIDATE CACHE
            InvalidateSiteCache();

            return new SiteDto
            {
                SiteID = site.SiteID,
                Name = site.Name,
                Location = site.Location,
                Email = site.Email,
                Contact = site.Contact,
                Status = site.Status
            };
        }

        // ❌ NO DELETE METHOD — Sites are kept for historical records

        private void InvalidateSiteCache()
        {
            // Clear all site caches (brute force)
            for (int i = 0; i < 100; i++)
            {
                string cacheKey = string.Format(SITE_CACHE_KEY, $"*{i}", $"*{i}", $"*{i}");
                _cache.Remove(cacheKey);
            }
        }
    }
}