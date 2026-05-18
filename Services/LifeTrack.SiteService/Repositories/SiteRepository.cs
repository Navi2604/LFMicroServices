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

        public async Task<List<SiteDto>> GetAllAsync(SiteFilterDto filter)
        {
            var query = _db.Sites.AsQueryable();

            if (!string.IsNullOrEmpty(filter.Name))
                query = query.Where(s => s.Name.Contains(filter.Name));

            if (!string.IsNullOrEmpty(filter.Location))
                query = query.Where(s => s.Location.Contains(filter.Location));

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(s => s.Status == filter.Status);

            return await query
                .OrderByDescending(s => s.SiteID)
                .Select(s => new SiteDto
                {
                    SiteID = s.SiteID,
                    Name = s.Name,
                    Location = s.Location,
                    Status = s.Status
                })
                .ToListAsync();
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
                Status = s.Status
            };
        }

        public async Task<SiteDto> CreateAsync(CreateSiteRequest req)
        {
            var site = new Site
            {
                Name = req.Name,
                Location = req.Location,
                Status = req.Status
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