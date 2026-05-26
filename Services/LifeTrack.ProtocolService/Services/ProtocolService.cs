// ============================================================
// ProtocolService.API / Services / ProtocolService.cs
// ADDED: UnarchiveAsync
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

        private static string ComputeStatus(DateTime start, DateTime end)
        {
            var today = DateTime.Today;
            if (today > end.Date) return "Completed";
            if (today >= start.Date) return "Ongoing";
            return "Upcoming";
        }

        public async Task<ApiResponse<List<ProtocolDto>>> GetAllAsync(ProtocolFilterDto filter)
            => ApiResponse<List<ProtocolDto>>.Ok(await _repo.GetAllAsync(filter));

        public async Task<ApiResponse<ProtocolDto>> GetByIdAsync(long id)
        {
            var p = await _repo.GetByIdAsync(id);
            return p == null
                ? ApiResponse<ProtocolDto>.Fail($"Protocol {id} not found.")
                : ApiResponse<ProtocolDto>.Ok(p);
        }

        public async Task<ApiResponse<ProtocolDto>> CreateAsync(CreateProtocolRequest req)
        {
            var status = ComputeStatus(req.StartDate, req.EndDate);
            var result = await _repo.CreateAsync(req, status);
            return ApiResponse<ProtocolDto>.Ok(result);
        }

        public async Task<ApiResponse<bool>> UpdateAsync(long id, UpdateProtocolRequest req)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                return ApiResponse<bool>.Fail($"Protocol {id} not found.");

            var status = ComputeStatus(req.StartDate, req.EndDate);
            var success = await _repo.UpdateAsync(id, req, status);
            return success
                ? ApiResponse<bool>.Ok(true)
                : ApiResponse<bool>.Fail("Update failed.");
        }

        public async Task<ApiResponse<bool>> ArchiveAsync(long id)
        {
            try
            {
                var ok = await _repo.ArchiveAsync(id);
                return ok
                    ? ApiResponse<bool>.Ok(true, "Protocol archived successfully.")
                    : ApiResponse<bool>.Fail("Protocol not found.");
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse<bool>.Fail(ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> UnarchiveAsync(long id)
        {
            try
            {
                var ok = await _repo.UnarchiveAsync(id);
                return ok
                    ? ApiResponse<bool>.Ok(true, "Protocol restored successfully.")
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
                var ok = await _repo.DeleteAsync(id);
                return ok
                    ? ApiResponse<bool>.Ok(true, "Protocol permanently deleted.")
                    : ApiResponse<bool>.Fail($"Protocol {id} not found.");
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse<bool>.Fail(ex.Message);
            }
        }
    }
}