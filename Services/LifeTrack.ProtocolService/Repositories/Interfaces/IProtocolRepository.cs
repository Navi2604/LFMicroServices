// ============================================================
// ProtocolService.API / Repositories / Interfaces / IProtocolRepository.cs
// ADDED: UnarchiveAsync
// ============================================================

using LifeTrack.ProtocolService.DTOs;

namespace LifeTrack.ProtocolService.Repositories.Interfaces
{
    public interface IProtocolRepository
    {
        Task<List<ProtocolDto>> GetAllAsync(ProtocolFilterDto filter);
        Task<ProtocolDto?> GetByIdAsync(long id);
        Task<ProtocolDto> CreateAsync(CreateProtocolRequest req, string computedStatus);
        Task<bool> UpdateAsync(long id, UpdateProtocolRequest req, string computedStatus);
        Task<bool> ArchiveAsync(long id);
        Task<bool> UnarchiveAsync(long id);   // restores Archived → computed status
        Task<bool> DeleteAsync(long id);
    }
}