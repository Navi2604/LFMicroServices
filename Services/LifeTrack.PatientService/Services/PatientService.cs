using LifeTrack.PatientService.DTOs;
using LifeTrack.PatientService.Services.Interfaces;
using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using LifeTrack.Shared.Wrappers;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.PatientService.Services
{
    public class PatientService : IPatientService
    {
        private readonly LifeTrackDbContext _db;
        public PatientService(LifeTrackDbContext db)
            => _db = db;

        public async Task<ApiResponse<List<PatientDto>>>
            GetAllAsync()
        {
            var list = await _db.Patients.ToListAsync();
            return ApiResponse<List<PatientDto>>.Ok(
                list.Select(MapToDto).ToList());
        }

        public async Task<ApiResponse<PatientDto>>
            GetByIdAsync(long id)
        {
            var p = await _db.Patients.FindAsync(id);
            if (p == null)
                return ApiResponse<PatientDto>.Fail(
                    "Patient not found.");
            return ApiResponse<PatientDto>.Ok(MapToDto(p));
        }

        public async Task<ApiResponse<PatientDto>> EnrollAsync(
            EnrollPatientRequest req, long investigatorId)
        {
            // Check email not already used
            var exists = await _db.Patients.AnyAsync(p =>
                p.Email != null &&
                p.Email.ToLower() == req.Email.ToLower());
            if (exists)
                return ApiResponse<PatientDto>.Fail(
                    "Email already registered.");

            var patient = new Patient
            {
                Name = req.Name,
                Email = req.Email,
                DOB = req.DOB,
                ContactInfo = req.ContactInfo,
                EnrollmentStatus = "Active",
                EnrolledBy = investigatorId
            };

            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();

            return ApiResponse<PatientDto>.Ok(
                MapToDto(patient), "Patient enrolled.");
        }

        public async Task<ApiResponse<PatientDto>>
            UpdateStatusAsync(long id, UpdateStatusRequest req)
        {
            var p = await _db.Patients.FindAsync(id);
            if (p == null)
                return ApiResponse<PatientDto>.Fail(
                    "Patient not found.");

            p.EnrollmentStatus = req.EnrollmentStatus;
            await _db.SaveChangesAsync();

            return ApiResponse<PatientDto>.Ok(
                MapToDto(p), "Status updated.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            var p = await _db.Patients.FindAsync(id);
            if (p == null)
                return ApiResponse<bool>.Fail(
                    "Patient not found.");

            _db.Patients.Remove(p);
            await _db.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Patient deleted.");
        }

        private static PatientDto MapToDto(Patient p) => new()
        {
            PatientID = p.PatientID,
            Name = p.Name,
            DOB = p.DOB,
            ContactInfo = p.ContactInfo,
            Email = p.Email,
            EnrollmentStatus = p.EnrollmentStatus,
            EnrolledBy = p.EnrolledBy
        };
    }
}