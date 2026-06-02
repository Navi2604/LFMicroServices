// ============================================================
// ProtocolService.API / Services / KPIReportService.cs
// ============================================================

using LifeTrack.ProtocolService.DTOs;
using LifeTrack.ProtocolService.Repositories.Interfaces;
using LifeTrack.ProtocolService.Services.Interfaces;
using LifeTrack.Shared.Helpers;
using LifeTrack.Shared.Wrappers;

namespace LifeTrack.ProtocolService.Services
{
    public class KPIReportService : IKPIReportService
    {
        private readonly IKPIReportRepository _repo;
        private readonly AuditHttpClient _audit;

        public KPIReportService(IKPIReportRepository repo, AuditHttpClient audit)
        {
            _repo = repo;
            _audit = audit;
        }

        public async Task<ApiResponse<List<KPIReportDto>>> GetAllAsync(KPIReportFilterDto filter)
            => ApiResponse<List<KPIReportDto>>.Ok(await _repo.GetAllAsync(filter));

        public async Task<ApiResponse<KPIReportDto>> GetByIdAsync(long id)
        {
            var report = await _repo.GetByIdAsync(id);
            return report == null
                ? ApiResponse<KPIReportDto>.Fail($"KPI report with ID {id} not found.")
                : ApiResponse<KPIReportDto>.Ok(report);
        }

        public async Task<ApiResponse<KPIReportDto>> CreateAsync(CreateKPIReportRequest req)
        {
            var report = await _repo.CreateAsync(req);

            _audit.Log("CREATE", "KPIReport", report.ReportID,
                $"KPI report generated for protocol '{report.ProtocolTitle}'. " +
                $"Enrollment rate: {report.EnrollmentRate}%, Dropout rate: {report.DropoutRate}%.");

            return ApiResponse<KPIReportDto>.Ok(report, "KPI report generated successfully.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            var deleted = await _repo.DeleteAsync(id);

            if (deleted)
                _audit.Log("DELETE", "KPIReport", id,
                    $"KPI report ID {id} deleted.");

            return deleted
                ? ApiResponse<bool>.Ok(true, "KPI report deleted.")
                : ApiResponse<bool>.Fail($"KPI report with ID {id} not found.");
        }
    }
}