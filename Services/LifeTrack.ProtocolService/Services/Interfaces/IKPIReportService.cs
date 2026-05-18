// ============================================================
// ProtocolService.API / Services / Interfaces / IKPIReportService.cs
// ============================================================

using LifeTrack.ProtocolService.DTOs;
using LifeTrack.Shared.Wrappers;

namespace LifeTrack.ProtocolService.Services.Interfaces
{
    public interface IKPIReportService
    {
        Task<ApiResponse<List<KPIReportDto>>> GetAllAsync(KPIReportFilterDto filter);
        Task<ApiResponse<KPIReportDto>> GetByIdAsync(long id);
        Task<ApiResponse<KPIReportDto>> CreateAsync(CreateKPIReportRequest req);
        Task<ApiResponse<bool>> DeleteAsync(long id);
    }
}