// ============================================================
// ProtocolService.API / Repositories / KPIReportRepository.cs
// ============================================================

using LifeTrack.ProtocolService.DTOs;
using LifeTrack.ProtocolService.Repositories.Interfaces;
using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.ProtocolService.Repositories
{
    public class KPIReportRepository : IKPIReportRepository
    {
        private readonly LifeTrackDbContext _db;

        public KPIReportRepository(LifeTrackDbContext db) => _db = db;

        public async Task<List<KPIReportDto>> GetAllAsync(KPIReportFilterDto filter)
        {
            var query = _db.KPIReports
                .Include(k => k.Protocol)
                .AsQueryable();

            if (filter.ProtocolID.HasValue)
                query = query.Where(k => k.ProtocolID == filter.ProtocolID.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(k => k.GeneratedDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(k => k.GeneratedDate <= filter.ToDate.Value);

            return await query
                .OrderByDescending(k => k.GeneratedDate)
                .Select(k => new KPIReportDto
                {
                    ReportID = k.ReportID,
                    ProtocolID = k.ProtocolID,
                    ProtocolTitle = k.Protocol != null ? k.Protocol.Title : "",
                    Scope = k.Scope,
                    EnrollmentRate = k.EnrollmentRate,
                    DropoutRate = k.DropoutRate,
                    AECount = k.AECount,
                    GeneratedDate = k.GeneratedDate
                })
                .ToListAsync();
        }

        public async Task<KPIReportDto?> GetByIdAsync(long id)
        {
            var k = await _db.KPIReports
                .Include(x => x.Protocol)
                .FirstOrDefaultAsync(x => x.ReportID == id);

            if (k == null) return null;

            return new KPIReportDto
            {
                ReportID = k.ReportID,
                ProtocolID = k.ProtocolID,
                ProtocolTitle = k.Protocol?.Title ?? "",
                Scope = k.Scope,
                EnrollmentRate = k.EnrollmentRate,
                DropoutRate = k.DropoutRate,
                AECount = k.AECount,
                GeneratedDate = k.GeneratedDate
            };
        }

        public async Task<KPIReportDto> CreateAsync(CreateKPIReportRequest req)
        {
            var report = new KPIReport
            {
                ProtocolID = req.ProtocolID,
                Scope = req.Scope,
                EnrollmentRate = req.EnrollmentRate,
                DropoutRate = req.DropoutRate,
                AECount = req.AECount,
                GeneratedDate = DateTime.UtcNow
            };

            _db.KPIReports.Add(report);
            await _db.SaveChangesAsync();

            return (await GetByIdAsync(report.ReportID))!;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var report = await _db.KPIReports.FindAsync(id);
            if (report == null) return false;

            _db.KPIReports.Remove(report);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}