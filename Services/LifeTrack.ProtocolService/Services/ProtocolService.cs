// ============================================================
// ProtocolService.API / Services / ProtocolService.cs
// ============================================================

using LifeTrack.ProtocolService.DTOs;
using LifeTrack.ProtocolService.Repositories.Interfaces;
using LifeTrack.ProtocolService.Services.Interfaces;
using LifeTrack.Shared.Helpers;
using LifeTrack.Shared.Wrappers;

namespace LifeTrack.ProtocolService.Services
{
    public class ProtocolService : IProtocolService
    {
        private readonly IProtocolRepository _repo;
        private readonly AuditHttpClient _audit;

        public ProtocolService(IProtocolRepository repo, AuditHttpClient audit)
        {
            _repo = repo;
            _audit = audit;
        }

        private static string ComputeStatus(DateTime startDate, DateTime endDate)
        {
            var today = DateTime.Today;
            if (today > endDate.Date) return "Completed";
            if (today >= startDate.Date) return "Ongoing";
            return "Upcoming";
        }

        public async Task<ApiResponse<List<ProtocolDto>>> GetAllAsync(ProtocolFilterDto filter)
            => ApiResponse<List<ProtocolDto>>.Ok(await _repo.GetAllAsync(filter));

        public async Task<ApiResponse<ProtocolDto>> GetByIdAsync(long id)
        {
            var protocol = await _repo.GetByIdAsync(id);
            if (protocol == null)
                return ApiResponse<ProtocolDto>.Fail($"Protocol with ID {id} not found.");
            return ApiResponse<ProtocolDto>.Ok(protocol);
        }

        public async Task<ApiResponse<ProtocolDto>> CreateAsync(CreateProtocolRequest req)
        {
            var computedStatus = ComputeStatus(req.StartDate, req.EndDate);
            var result = await _repo.CreateAsync(req, computedStatus);

            _audit.Log("CREATE", "Protocol", result.ProtocolID,
                $"Protocol '{result.Title}' created with status '{result.Status}'.");

            return ApiResponse<ProtocolDto>.Ok(result);
        }

        public async Task<ApiResponse<bool>> UpdateAsync(long id, UpdateProtocolRequest req)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                return ApiResponse<bool>.Fail($"Protocol with ID {id} not found.");

            var computedStatus = ComputeStatus(req.StartDate, req.EndDate);
            var success = await _repo.UpdateAsync(id, req, computedStatus);

            if (success)
                _audit.Log("UPDATE", "Protocol", id,
                    $"Protocol '{req.Title}' updated. Status: '{computedStatus}'.");

            return success
                ? ApiResponse<bool>.Ok(true)
                : ApiResponse<bool>.Fail("Update failed.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            var existing = await _repo.GetByIdAsync(id);
            var deleted = await _repo.DeleteAsync(id);

            if (deleted)
                _audit.Log("DELETE", "Protocol", id,
                    $"Protocol '{existing?.Title}' deleted.");

            return deleted
                ? ApiResponse<bool>.Ok(true)
                : ApiResponse<bool>.Fail($"Protocol with ID {id} not found.");
        }
    }
}