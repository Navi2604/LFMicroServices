// ============================================================
// ProtocolService.API / Repositories / Interfaces / IKPIReportRepository.cs
// ============================================================

using LifeTrack.ProtocolService.DTOs;

namespace LifeTrack.ProtocolService.Repositories.Interfaces
{
    public interface IKPIReportRepository
    {
        Task<List<KPIReportDto>> GetAllAsync(KPIReportFilterDto filter);
        Task<KPIReportDto?> GetByIdAsync(long id);
        Task<KPIReportDto> CreateAsync(CreateKPIReportRequest req);
        Task<bool> DeleteAsync(long id);
    }
}