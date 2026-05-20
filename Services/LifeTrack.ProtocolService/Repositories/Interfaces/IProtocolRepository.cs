// ============================================================
// ProtocolService.API / Repositories / Interfaces / IProtocolRepository.cs
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
        Task<bool> DeleteAsync(long id);
    }
}