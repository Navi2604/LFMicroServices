// ============================================================
// ProtocolService.API / Services / Interfaces / IProtocolService.cs
// ADDED: UnarchiveAsync
// ============================================================

using LifeTrack.ProtocolService.DTOs;
using LifeTrack.Shared.Wrappers;

namespace LifeTrack.ProtocolService.Services.Interfaces
{
    public interface IProtocolService
    {
        Task<ApiResponse<List<ProtocolDto>>> GetAllAsync(ProtocolFilterDto filter);
        Task<ApiResponse<ProtocolDto>> GetByIdAsync(long id);
        Task<ApiResponse<ProtocolDto>> CreateAsync(CreateProtocolRequest req);
        Task<ApiResponse<bool>> UpdateAsync(long id, UpdateProtocolRequest req);
        Task<ApiResponse<bool>> ArchiveAsync(long id);
        Task<ApiResponse<bool>> UnarchiveAsync(long id);  // restores Archived → computed status
        Task<ApiResponse<bool>> DeleteAsync(long id);
    }
}