// ============================================================
// SiteService.API / Repositories / SiteRepository.cs
// ============================================================

using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using LifeTrack.SiteService.DTOs;
using LifeTrack.SiteService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.SiteService.Repositories
{
    public class SiteRepository : ISiteRepository
    {
        private readonly LifeTrackDbContext _db;

        public SiteRepository(LifeTrackDbContext db) => _db = db;

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

            // Normalise statuses on the way out
            return sites.Select(s => new SiteDto
            {
                SiteID = s.SiteID,
                Name = s.Name,
                Location = s.Location,
                Status = NormaliseStatus(s.Status)
            }).ToList();
        }

        public async Task<SiteDto?> GetByIdAsync(long id)
        {
            var s = await _db.Sites.FindAsync(id);
            if (s == null) return null;

            return new SiteDto
            {
                SiteID = s.SiteID,
                Name = s.Name,
                Location = s.Location,
                Status = NormaliseStatus(s.Status)
            };
        }

        public async Task<SiteDto> CreateAsync(CreateSiteRequest req)
        {
            var site = new Site
            {
                Name = req.Name,
                Location = req.Location,
                Status = NormaliseStatus(req.Status)
            };

            _db.Sites.Add(site);
            await _db.SaveChangesAsync();

            return new SiteDto
            {
                SiteID = site.SiteID,
                Name = site.Name,
                Location = site.Location,
                Status = site.Status
            };
        }

        public async Task<SiteDto?> UpdateAsync(long id, UpdateSiteRequest req)
        {
            var site = await _db.Sites.FindAsync(id);
            if (site == null) return null;

            site.Name = req.Name;
            site.Location = req.Location;
            site.Status = NormaliseStatus(req.Status);

            await _db.SaveChangesAsync();

            return new SiteDto
            {
                SiteID = site.SiteID,
                Name = site.Name,
                Location = site.Location,
                Status = site.Status
            };
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var s = await _db.Sites.FindAsync(id);
            if (s == null) return false;

            _db.Sites.Remove(s);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}