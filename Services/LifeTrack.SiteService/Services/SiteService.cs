using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using LifeTrack.Shared.Wrappers;
using LifeTrack.SiteService.DTOs;
using LifeTrack.SiteService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.SiteService.Services
{
    public class SiteService : ISiteService
    {
        private readonly LifeTrackDbContext _db;
        public SiteService(LifeTrackDbContext db) => _db = db;

        public async Task<ApiResponse<List<SiteDto>>> GetAllAsync()
        {
            var list = await _db.Sites.ToListAsync();
            return ApiResponse<List<SiteDto>>.Ok(
                list.Select(MapToDto).ToList());
        }

        public async Task<ApiResponse<SiteDto>> GetByIdAsync(
            long id)
        {
            var s = await _db.Sites.FindAsync(id);
            if (s == null)
                return ApiResponse<SiteDto>.Fail(
                    "Site not found.");
            return ApiResponse<SiteDto>.Ok(MapToDto(s));
        }

        public async Task<ApiResponse<SiteDto>> CreateAsync(
            CreateSiteRequest req)
        {
            var site = new Site
            {
                Name = req.Name,
                Location = req.Location,
                InvestigatorID = req.InvestigatorID,
                ProtocolID = req.ProtocolID,
                Status = req.Status
            };
            _db.Sites.Add(site);
            await _db.SaveChangesAsync();
            return ApiResponse<SiteDto>.Ok(
                MapToDto(site), "Site created.");
        }

        public async Task<ApiResponse<SiteDto>> UpdateAsync(
            long id, CreateSiteRequest req)
        {
            var s = await _db.Sites.FindAsync(id);
            if (s == null)
                return ApiResponse<SiteDto>.Fail(
                    "Site not found.");

            s.Name = req.Name;
            s.Location = req.Location;
            s.InvestigatorID = req.InvestigatorID;
            s.ProtocolID = req.ProtocolID;
            s.Status = req.Status;

            await _db.SaveChangesAsync();
            return ApiResponse<SiteDto>.Ok(
                MapToDto(s), "Site updated.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            var s = await _db.Sites.FindAsync(id);
            if (s == null)
                return ApiResponse<bool>.Fail(
                    "Site not found.");
            _db.Sites.Remove(s);
            await _db.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Site deleted.");
        }

        private static SiteDto MapToDto(Site s) => new()
        {
            SiteID = s.SiteID,
            Name = s.Name,
            Location = s.Location,
            InvestigatorID = s.InvestigatorID,
            ProtocolID = s.ProtocolID,
            Status = s.Status
        };
    }
}