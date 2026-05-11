using LifeTrack.ProtocolService.DTOs;
using LifeTrack.Shared.Wrappers;

namespace LifeTrack.ProtocolService.Services.Interfaces
{
    public interface IProtocolService
    {
        Task<ApiResponse<List<ProtocolDto>>> GetAllAsync();
        Task<ApiResponse<ProtocolDto>> GetByIdAsync(long id);
        Task<ApiResponse<ProtocolDto>> CreateAsync(
            CreateProtocolRequest req);
        Task<ApiResponse<ProtocolDto>> UpdateAsync(
            long id, CreateProtocolRequest req);
        Task<ApiResponse<bool>> DeleteAsync(long id);
    }
}