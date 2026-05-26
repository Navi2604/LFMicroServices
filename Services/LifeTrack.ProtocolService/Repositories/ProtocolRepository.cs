// ============================================================
// ProtocolService.API / Repositories / ProtocolRepository.cs
// FIXED:
// - ArchiveAsync: allows ANY non-Archived status (was Upcoming only)
// - UnarchiveAsync: restores Archived → Upcoming/Ongoing/Completed
//   based on today's date vs start/end dates
// ============================================================

using System.Text.Json;
using LifeTrack.ProtocolService.DTOs;
using LifeTrack.ProtocolService.Repositories.Interfaces;
using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LifeTrack.ProtocolService.Repositories
{
    public class ProtocolRepository : IProtocolRepository
    {
        private readonly LifeTrackDbContext _context;
        private readonly IMemoryCache _cache;

        private const string LIST_KEY = "protocols_{0}_{1}_{2}_{3}";
        private const string SINGLE_KEY = "protocol_{0}";
        private const int TTL = 20; // minutes

        public ProtocolRepository(LifeTrackDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // ── GetAll ────────────────────────────────────────────────
        public async Task<List<ProtocolDto>> GetAllAsync(ProtocolFilterDto filter)
        {
            // Always sync stale statuses first (non-Archived protocols whose
            // computed status no longer matches what is stored in the DB).
            await SyncStaleStatusesAsync();

            var key = string.Format(LIST_KEY,
                filter.Title ?? "null",
                filter.Status ?? "null",
                filter.FromDate?.ToString("yyyy-MM-dd") ?? "null",
                filter.ToDate?.ToString("yyyy-MM-dd") ?? "null");

            if (_cache.TryGetValue(key, out List<ProtocolDto>? hit)) return hit!;

            var q = _context.Protocols.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Title))
                q = q.Where(p => p.Title.Contains(filter.Title));
            if (!string.IsNullOrWhiteSpace(filter.Status))
                q = q.Where(p => p.Status == filter.Status);
            if (filter.FromDate.HasValue)
                q = q.Where(p => p.StartDate >= filter.FromDate.Value);
            if (filter.ToDate.HasValue)
                q = q.Where(p => p.EndDate <= filter.ToDate.Value);

            var list = (await q.ToListAsync()).Select(MapToDto).ToList();
            _cache.Set(key, list, TimeSpan.FromMinutes(TTL));
            return list;
        }

        // ── GetById ───────────────────────────────────────────────
        public async Task<ProtocolDto?> GetByIdAsync(long id)
        {
            var key = string.Format(SINGLE_KEY, id);
            if (_cache.TryGetValue(key, out ProtocolDto? hit)) return hit;

            var p = await _context.Protocols.FindAsync(id);
            if (p == null) return null;

            // Recompute and persist status if it has drifted
            if (p.Status != "Archived")
            {
                var correct = ComputeStatus(p.StartDate, p.EndDate);
                if (p.Status != correct)
                {
                    p.Status = correct;
                    await _context.SaveChangesAsync();
                    BustCache(id);
                }
            }

            var dto = MapToDto(p);
            _cache.Set(key, dto, TimeSpan.FromMinutes(TTL));
            return dto;
        }

        // ── Create ────────────────────────────────────────────────
        public async Task<ProtocolDto> CreateAsync(
            CreateProtocolRequest req, string computedStatus)
        {
            var p = new Protocol
            {
                Title = req.Title,
                StartDate = req.StartDate,
                EndDate = req.EndDate,
                Status = computedStatus,
                PhasesJson = req.Phases?.Count > 0
                    ? JsonSerializer.Serialize(req.Phases) : null
            };

            _context.Protocols.Add(p);
            await _context.SaveChangesAsync();
            BustCache();
            return MapToDto(p);
        }

        // ── Update ────────────────────────────────────────────────
        public async Task<bool> UpdateAsync(
            long id, UpdateProtocolRequest req, string computedStatus)
        {
            var p = await _context.Protocols.FindAsync(id);
            if (p == null) return false;

            p.Title = req.Title;
            p.StartDate = req.StartDate;
            p.EndDate = req.EndDate;
            p.Status = computedStatus;

            if (req.Phases?.Count > 0)
                p.PhasesJson = JsonSerializer.Serialize(req.Phases);

            await _context.SaveChangesAsync();
            BustCache(id);
            return true;
        }

        // ── Archive ───────────────────────────────────────────────
        // FIXED: allow archiving from ANY status (not just Upcoming)
        public async Task<bool> ArchiveAsync(long id)
        {
            var p = await _context.Protocols.FindAsync(id);
            if (p == null) return false;

            if (p.Status == "Archived")
                throw new InvalidOperationException(
                    "This protocol is already archived.");

            p.Status = "Archived";
            await _context.SaveChangesAsync();
            BustCache(id);
            return true;
        }

        // ── Unarchive ─────────────────────────────────────────────
        // Restores an Archived protocol to its correct status
        // based on today's date vs the protocol's start/end dates.
        public async Task<bool> UnarchiveAsync(long id)
        {
            var p = await _context.Protocols.FindAsync(id);
            if (p == null) return false;

            if (p.Status != "Archived")
                throw new InvalidOperationException(
                    "Only Archived protocols can be unarchived.");

            // Recompute status from dates
            p.Status = ComputeStatus(p.StartDate, p.EndDate);
            await _context.SaveChangesAsync();
            BustCache(id);
            return true;
        }

        // ── Delete (Archived only) ────────────────────────────────
        public async Task<bool> DeleteAsync(long id)
        {
            var p = await _context.Protocols
                .Include(x => x.SiteProtocols)
                    .ThenInclude(sp => sp.Enrollments)
                        .ThenInclude(e => e.Visits)
                .FirstOrDefaultAsync(x => x.ProtocolID == id);

            if (p == null) return false;

            if (p.Status != "Archived")
                throw new InvalidOperationException(
                    "Only Archived protocols can be permanently deleted. Archive it first.");

            var investigatorIds = p.SiteProtocols
                .Select(sp => sp.InvestigatorID).Distinct().ToList();

            foreach (var sp in p.SiteProtocols)
            {
                foreach (var e in sp.Enrollments)
                    _context.Visits.RemoveRange(e.Visits);
                _context.Enrollments.RemoveRange(sp.Enrollments);
            }
            _context.SiteProtocols.RemoveRange(p.SiteProtocols);
            _context.Protocols.Remove(p);
            await _context.SaveChangesAsync();

            // Deactivate investigators who have no remaining assignments
            foreach (var invId in investigatorIds)
            {
                bool hasOther = await _context.SiteProtocols
                    .AnyAsync(sp => sp.InvestigatorID == invId);
                if (!hasOther)
                {
                    var inv = await _context.Users.FindAsync(invId);
                    if (inv != null) inv.IsActive = false;
                }
            }
            await _context.SaveChangesAsync();

            BustCache(id);
            return true;
        }

        // ── Sync stale statuses ───────────────────────────────────
        // Finds any non-Archived protocol whose stored status no longer
        // matches the date-computed status and updates it in the DB.
        // Called on every GetAllAsync so the list is always fresh.
        private async Task SyncStaleStatusesAsync()
        {
            var stale = await _context.Protocols
                .Where(p => p.Status != "Archived")
                .ToListAsync();

            bool any = false;
            foreach (var p in stale)
            {
                var correct = ComputeStatus(p.StartDate, p.EndDate);
                if (p.Status != correct)
                {
                    p.Status = correct;
                    any = true;
                    // Bust single-item cache
                    _cache.Remove(string.Format(SINGLE_KEY, p.ProtocolID));
                }
            }

            if (any)
            {
                await _context.SaveChangesAsync();
                // Bust all list caches so fresh data is returned
                BustCache();
            }
        }

        // ── Helpers ───────────────────────────────────────────────
        private static string ComputeStatus(DateTime start, DateTime end)
        {
            var today = DateTime.Today;
            if (today > end.Date) return "Completed";
            if (today >= start.Date) return "Ongoing";
            return "Upcoming";
        }

        private static ProtocolDto MapToDto(Protocol p)
        {
            var phases = new List<PhaseDto>();
            if (!string.IsNullOrWhiteSpace(p.PhasesJson))
            {
                try { phases = JsonSerializer.Deserialize<List<PhaseDto>>(p.PhasesJson) ?? new(); }
                catch { phases = new(); }
            }

            return new ProtocolDto
            {
                ProtocolID = p.ProtocolID,
                Title = p.Title,
                Phase = phases.Count > 0
                    ? $"{phases.Count} Phase{(phases.Count > 1 ? "s" : "")}" : "",
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.Status,
                SiteCount = 0,
                Phases = phases
            };
        }

        private void BustCache(long? id = null)
        {
            if (id.HasValue)
                _cache.Remove(string.Format(SINGLE_KEY, id.Value));

            // Clear all list-cache permutations
            foreach (var s in new[] { "null", "Upcoming", "Ongoing", "Completed", "Archived" })
                _cache.Remove(string.Format(LIST_KEY, "null", s, "null", "null"));
            _cache.Remove(string.Format(LIST_KEY, "null", "null", "null", "null"));
        }
    }
}