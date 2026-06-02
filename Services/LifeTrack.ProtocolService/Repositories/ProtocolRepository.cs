// ============================================================
// ProtocolService.API / Repositories / ProtocolRepository.cs
// ============================================================

using System.Text.Json;
using LifeTrack.ProtocolService.DTOs;
using LifeTrack.ProtocolService.Repositories.Interfaces;
using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.ProtocolService.Repositories
{
    public class ProtocolRepository : IProtocolRepository
    {
        private readonly LifeTrackDbContext _context;

        public ProtocolRepository(LifeTrackDbContext context) => _context = context;

        // ── Get All (with optional filters) ──────────────────────────────────

        public async Task<List<ProtocolDto>> GetAllAsync(ProtocolFilterDto filter)
        {
            var query = _context.Protocols.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Title))
                query = query.Where(p => p.Title.Contains(filter.Title));

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(p => p.Status == filter.Status);

            if (filter.FromDate.HasValue)
                query = query.Where(p => p.StartDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(p => p.EndDate <= filter.ToDate.Value);

            var protocols = await query.ToListAsync();
            return protocols.Select(MapToDto).ToList();
        }

        // ── Get By Id ─────────────────────────────────────────────────────────

        public async Task<ProtocolDto?> GetByIdAsync(long id)
        {
            var protocol = await _context.Protocols.FindAsync(id);
            return protocol == null ? null : MapToDto(protocol);
        }

        // ── Create ────────────────────────────────────────────────────────────

        public async Task<ProtocolDto> CreateAsync(CreateProtocolRequest req, string computedStatus)
        {
            var protocol = new Protocol
            {
                Title = req.Title,
                StartDate = req.StartDate,
                EndDate = req.EndDate,
                Status = computedStatus,   // Always server-calculated
                PhasesJson = req.Phases != null && req.Phases.Count > 0
                    ? JsonSerializer.Serialize(req.Phases)
                    : null
            };

            _context.Protocols.Add(protocol);
            await _context.SaveChangesAsync();
            return MapToDto(protocol);
        }

        // ── Update ────────────────────────────────────────────────────────────

        public async Task<bool> UpdateAsync(long id, UpdateProtocolRequest req, string computedStatus)
        {
            var protocol = await _context.Protocols.FindAsync(id);
            if (protocol == null) return false;

            protocol.Title = req.Title;
            protocol.StartDate = req.StartDate;
            protocol.EndDate = req.EndDate;
            protocol.Status = computedStatus;   // Always server-calculated

            if (req.Phases != null && req.Phases.Count > 0)
                protocol.PhasesJson = JsonSerializer.Serialize(req.Phases);

            await _context.SaveChangesAsync();
            return true;
        }

        // ── Delete ────────────────────────────────────────────────────────────

        public async Task<bool> DeleteAsync(long id)
        {
            var protocol = await _context.Protocols.FindAsync(id);
            if (protocol == null) return false;
            _context.Protocols.Remove(protocol);
            await _context.SaveChangesAsync();
            return true;
        }

        // ── Mapper ────────────────────────────────────────────────────────────

        private static ProtocolDto MapToDto(Protocol protocol)
        {
            var phases = new List<PhaseDto>();
            if (!string.IsNullOrWhiteSpace(protocol.PhasesJson))
            {
                try { phases = JsonSerializer.Deserialize<List<PhaseDto>>(protocol.PhasesJson) ?? new(); }
                catch { phases = new(); }
            }

            // Build a human-readable Phase summary e.g. "3 Phases" for the list view
            var phaseSummary = phases.Count > 0 ? $"{phases.Count} Phase{(phases.Count > 1 ? "s" : "")}" : "";

            return new ProtocolDto
            {
                ProtocolID = protocol.ProtocolID,
                Title = protocol.Title,
                Phase = phaseSummary,
                StartDate = protocol.StartDate,
                EndDate = protocol.EndDate,
                Status = protocol.Status,
                SiteCount = 0,   // Populated separately if needed via SiteService
                Phases = phases
            };
        }
    }
}