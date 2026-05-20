// ============================================================
// ProtocolService.API / Services / ProtocolService.cs
// ============================================================

using LifeTrack.ProtocolService.DTOs;
using LifeTrack.ProtocolService.Repositories.Interfaces;
using LifeTrack.ProtocolService.Services.Interfaces;
using LifeTrack.Shared.Wrappers;

namespace LifeTrack.ProtocolService.Services
{
    public class ProtocolService : IProtocolService
    {
        private readonly IProtocolRepository _repo;

        public ProtocolService(IProtocolRepository repo) => _repo = repo;

        // ── Status calculation (always server-side) ───────────────────────────

        private static string ComputeStatus(DateTime startDate, DateTime endDate)
        {
            var today = DateTime.Today;
            if (today > endDate.Date) return "Completed";
            if (today >= startDate.Date) return "Ongoing";
            return "Upcoming";
        }

        // ── Get All ───────────────────────────────────────────────────────────

        public async Task<ApiResponse<List<ProtocolDto>>> GetAllAsync(ProtocolFilterDto filter)
        {
            var data = await _repo.GetAllAsync(filter);
            return ApiResponse<List<ProtocolDto>>.Ok(data);
        }

        // ── Get By Id ─────────────────────────────────────────────────────────

        public async Task<ApiResponse<ProtocolDto>> GetByIdAsync(long id)
        {
            var protocol = await _repo.GetByIdAsync(id);
            if (protocol == null)
                return ApiResponse<ProtocolDto>.Fail($"Protocol with ID {id} not found.");
            return ApiResponse<ProtocolDto>.Ok(protocol);
        }

        // ── Create ────────────────────────────────────────────────────────────

        public async Task<ApiResponse<ProtocolDto>> CreateAsync(CreateProtocolRequest req)
        {
            var computedStatus = ComputeStatus(req.StartDate, req.EndDate);
            var result = await _repo.CreateAsync(req, computedStatus);
            return ApiResponse<ProtocolDto>.Ok(result);
        }

        // ── Update ────────────────────────────────────────────────────────────

        public async Task<ApiResponse<bool>> UpdateAsync(long id, UpdateProtocolRequest req)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                return ApiResponse<bool>.Fail($"Protocol with ID {id} not found.");

            var computedStatus = ComputeStatus(req.StartDate, req.EndDate);
            var success = await _repo.UpdateAsync(id, req, computedStatus);
            return success
                ? ApiResponse<bool>.Ok(true)
                : ApiResponse<bool>.Fail("Update failed.");
        }

        // ── Delete ────────────────────────────────────────────────────────────

        public async Task<ApiResponse<bool>> ArchiveAsync(long id)
        {
            try
            {
                var archived = await _repo.ArchiveAsync(id);
                return archived
                    ? ApiResponse<bool>.Ok(true, "Protocol archived successfully.")
                    : ApiResponse<bool>.Fail("Protocol not found.");
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse<bool>.Fail(ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            try
            {
                var deleted = await _repo.DeleteAsync(id);
                return deleted
                    ? ApiResponse<bool>.Ok(true, "Protocol permanently deleted.")
                    : ApiResponse<bool>.Fail($"Protocol with ID {id} not found.");
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse<bool>.Fail(ex.Message);
            }
        }
    }
}