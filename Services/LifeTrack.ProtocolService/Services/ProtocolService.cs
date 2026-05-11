using LifeTrack.ProtocolService.DTOs;
using LifeTrack.ProtocolService.Services.Interfaces;
using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using LifeTrack.Shared.Wrappers;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.ProtocolService.Services
{
    public class ProtocolService : IProtocolService
    {
        private readonly LifeTrackDbContext _db;
        public ProtocolService(LifeTrackDbContext db)
            => _db = db;

        public async Task<ApiResponse<List<ProtocolDto>>>
            GetAllAsync()
        {
            var list = await _db.Protocols.ToListAsync();
            return ApiResponse<List<ProtocolDto>>.Ok(
                list.Select(MapToDto).ToList());
        }

        public async Task<ApiResponse<ProtocolDto>>
            GetByIdAsync(long id)
        {
            var p = await _db.Protocols.FindAsync(id);
            if (p == null)
                return ApiResponse<ProtocolDto>.Fail(
                    "Protocol not found.");
            return ApiResponse<ProtocolDto>.Ok(MapToDto(p));
        }

        public async Task<ApiResponse<ProtocolDto>> CreateAsync(
            CreateProtocolRequest req)
        {
            var protocol = new Protocol
            {
                Title = req.Title,
                Phase = req.Phase,
                StartDate = req.StartDate,
                EndDate = req.EndDate,
                Status = CalcStatus(
                    req.StartDate, req.EndDate),
                InvestigatorID = req.InvestigatorID,
                PhasesJson = req.PhasesJson
            };

            _db.Protocols.Add(protocol);
            await _db.SaveChangesAsync();

            return ApiResponse<ProtocolDto>.Ok(
                MapToDto(protocol), "Protocol created.");
        }

        public async Task<ApiResponse<ProtocolDto>> UpdateAsync(
            long id, CreateProtocolRequest req)
        {
            var p = await _db.Protocols.FindAsync(id);
            if (p == null)
                return ApiResponse<ProtocolDto>.Fail(
                    "Protocol not found.");

            p.Title = req.Title;
            p.Phase = req.Phase;
            p.StartDate = req.StartDate;
            p.EndDate = req.EndDate;
            p.Status = CalcStatus(
                req.StartDate, req.EndDate);
            p.InvestigatorID = req.InvestigatorID;
            p.PhasesJson = req.PhasesJson;

            await _db.SaveChangesAsync();
            return ApiResponse<ProtocolDto>.Ok(
                MapToDto(p), "Protocol updated.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            var p = await _db.Protocols.FindAsync(id);
            if (p == null)
                return ApiResponse<bool>.Fail(
                    "Protocol not found.");

            _db.Protocols.Remove(p);
            await _db.SaveChangesAsync();
            return ApiResponse<bool>.Ok(
                true, "Protocol deleted.");
        }

        private static string CalcStatus(
            DateTime start, DateTime end)
        {
            var now = DateTime.UtcNow;
            if (now < start) return "Upcoming";
            if (now > end) return "Completed";
            return "Ongoing";
        }

        private static ProtocolDto MapToDto(Protocol p) => new()
        {
            ProtocolID = p.ProtocolID,
            Title = p.Title,
            Phase = p.Phase,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Status = p.Status,
            InvestigatorID = p.InvestigatorID,
            PhasesJson = p.PhasesJson
        };
    }
}