using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using LifeTrack.Shared.Wrappers;
using LifeTrack.VisitService.DTOs;
using LifeTrack.VisitService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.VisitService.Services
{
    public class VisitService : IVisitService
    {
        private readonly LifeTrackDbContext _db;
        public VisitService(LifeTrackDbContext db) => _db = db;

        public async Task<ApiResponse<List<VisitDto>>> GetAllAsync()
        {
            var list = await _db.Visits.ToListAsync();
            return ApiResponse<List<VisitDto>>.Ok(
                list.Select(MapToDto).ToList());
        }

        public async Task<ApiResponse<List<VisitDto>>>
            GetByPatientAsync(long patientId)
        {
            var list = await _db.Visits
                .Where(v => v.PatientID == patientId)
                .ToListAsync();
            return ApiResponse<List<VisitDto>>.Ok(
                list.Select(MapToDto).ToList());
        }

        public async Task<ApiResponse<VisitDto>> GetByIdAsync(
            long id)
        {
            var v = await _db.Visits.FindAsync(id);
            if (v == null)
                return ApiResponse<VisitDto>.Fail(
                    "Visit not found.");
            return ApiResponse<VisitDto>.Ok(MapToDto(v));
        }

        public async Task<ApiResponse<VisitDto>> CreateAsync(
            CreateVisitRequest req)
        {
            var visit = new Visit
            {
                PatientID = req.PatientID,
                ProtocolID = req.ProtocolID,
                VisitDate = req.VisitDate,
                Status = req.Status,
                Notes = req.Notes
            };
            _db.Visits.Add(visit);
            await _db.SaveChangesAsync();
            return ApiResponse<VisitDto>.Ok(
                MapToDto(visit), "Visit created.");
        }

        public async Task<ApiResponse<VisitDto>> UpdateAsync(
            long id, CreateVisitRequest req)
        {
            var v = await _db.Visits.FindAsync(id);
            if (v == null)
                return ApiResponse<VisitDto>.Fail(
                    "Visit not found.");

            v.PatientID = req.PatientID;
            v.ProtocolID = req.ProtocolID;
            v.VisitDate = req.VisitDate;
            v.Status = req.Status;
            v.Notes = req.Notes;

            await _db.SaveChangesAsync();
            return ApiResponse<VisitDto>.Ok(
                MapToDto(v), "Visit updated.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            var v = await _db.Visits.FindAsync(id);
            if (v == null)
                return ApiResponse<bool>.Fail(
                    "Visit not found.");
            _db.Visits.Remove(v);
            await _db.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Visit deleted.");
        }

        private static VisitDto MapToDto(Visit v) => new()
        {
            VisitID = v.VisitID,
            PatientID = v.PatientID,
            ProtocolID = v.ProtocolID,
            VisitDate = v.VisitDate,
            Status = v.Status,
            Notes = v.Notes
        };
    }
}