using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LifeTrack.Shared;
using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace VisitService.API.Services
{
    public interface IVisitService
    {
        Task<List<VisitDto>> GetAllAsync();
        Task<VisitDto> GetByIdAsync(long id);
        Task<VisitDto> CreateAsync(CreateVisitRequest request);
        Task<bool> UpdateStatusAsync(long visitId, string status);
        Task<bool> DeleteAsync(long visitId);
    }

    public class VisitDto
    {
        public long VisitID { get; set; }
        public long EnrollmentID { get; set; }
        public string PatientName { get; set; }
        public string ProtocolTitle { get; set; }
        public DateTime VisitDate { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
    }

    public class CreateVisitRequest
    {
        public long EnrollmentID { get; set; }
        public DateTime VisitDate { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
    }

    public class VisitService : IVisitService
    {
        private readonly LifeTrackDbContext _db;

        public VisitService(LifeTrackDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Get all visits
        /// </summary>
        public async Task<List<VisitDto>> GetAllAsync()
        {
            try
            {
                var visits = await _db.Visits
                    .Include(v => v.Enrollment)
                        .ThenInclude(e => e.Patient)
                    .Include(v => v.Enrollment)
                        .ThenInclude(e => e.SiteProtocol)
                            .ThenInclude(sp => sp.Protocol)
                    .OrderBy(v => v.VisitID)
                    .ToListAsync();

                return visits.Select(v => new VisitDto
                {
                    VisitID = v.VisitID,
                    EnrollmentID = v.EnrollmentID,
                    PatientName = v.Enrollment?.Patient?.Name ?? "Unknown",
                    ProtocolTitle = v.Enrollment?.SiteProtocol?.Protocol?.Title ?? "Unknown",
                    VisitDate = v.VisitDate,
                    Status = v.Status,
                    Notes = v.Notes
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching visits: {ex.Message}");
                return new List<VisitDto>();
            }
        }

        /// <summary>
        /// Get visit by ID
        /// </summary>
        public async Task<VisitDto> GetByIdAsync(long id)
        {
            try
            {
                var visit = await _db.Visits
                    .Include(v => v.Enrollment)
                        .ThenInclude(e => e.Patient)
                    .Include(v => v.Enrollment)
                        .ThenInclude(e => e.SiteProtocol)
                            .ThenInclude(sp => sp.Protocol)
                    .FirstOrDefaultAsync(v => v.VisitID == id);

                if (visit == null)
                    return null;

                return new VisitDto
                {
                    VisitID = visit.VisitID,
                    EnrollmentID = visit.EnrollmentID,
                    PatientName = visit.Enrollment?.Patient?.Name ?? "Unknown",
                    ProtocolTitle = visit.Enrollment?.SiteProtocol?.Protocol?.Title ?? "Unknown",
                    VisitDate = visit.VisitDate,
                    Status = visit.Status,
                    Notes = visit.Notes
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching visit {id}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create a new visit
        /// </summary>
        public async Task<VisitDto> CreateAsync(CreateVisitRequest request)
        {
            try
            {
                // Validate enrollment exists
                var enrollment = await _db.Enrollments
                    .Include(e => e.Patient)
                    .Include(e => e.SiteProtocol)
                        .ThenInclude(sp => sp.Protocol)
                    .FirstOrDefaultAsync(e => e.EnrollmentID == request.EnrollmentID);

                if (enrollment == null)
                    throw new InvalidOperationException($"Enrollment {request.EnrollmentID} not found");

                // Validate enrollment status
                if (enrollment.Status != "Active" && enrollment.Status != "Pending")
                    throw new InvalidOperationException($"Cannot create visit for {enrollment.Status} enrollment");

                // Validate visit date is within protocol dates
                if (enrollment.SiteProtocol?.Protocol != null)
                {
                    var protocol = enrollment.SiteProtocol.Protocol;
                    if (request.VisitDate.Date < protocol.StartDate.Date || request.VisitDate.Date > protocol.EndDate.Date)
                        throw new InvalidOperationException(
                            $"Visit date {request.VisitDate:yyyy-MM-dd} is outside protocol date range {protocol.StartDate:yyyy-MM-dd} to {protocol.EndDate:yyyy-MM-dd}");
                }

                // Create visit
                var visit = new Visit
                {
                    EnrollmentID = request.EnrollmentID,
                    VisitDate = request.VisitDate,
                    Status = request.Status ?? "Scheduled",
                    Notes = request.Notes ?? string.Empty
                };

                _db.Visits.Add(visit);
                await _db.SaveChangesAsync();

                return new VisitDto
                {
                    VisitID = visit.VisitID,
                    EnrollmentID = visit.EnrollmentID,
                    PatientName = enrollment.Patient?.Name ?? "Unknown",
                    ProtocolTitle = enrollment.SiteProtocol?.Protocol?.Title ?? "Unknown",
                    VisitDate = visit.VisitDate,
                    Status = visit.Status,
                    Notes = visit.Notes
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating visit: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Update visit status
        /// </summary>
        public async Task<bool> UpdateStatusAsync(long visitId, string status)
        {
            try
            {
                var visit = await _db.Visits.FindAsync(visitId);
                if (visit == null)
                    return false;

                // Validate status
                var validStatuses = new[] { "Scheduled", "Completed", "Missed", "Cancelled" };
                if (!validStatuses.Contains(status))
                    throw new InvalidOperationException($"Invalid status: {status}");

                visit.Status = status;

                _db.Visits.Update(visit);
                await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating visit {visitId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Delete a visit (only Scheduled visits can be deleted)
        /// </summary>
        public async Task<bool> DeleteAsync(long visitId)
        {
            try
            {
                var visit = await _db.Visits.FindAsync(visitId);
                if (visit == null)
                {
                    Console.WriteLine($"Visit {visitId} not found");
                    return false;
                }

                // Only allow deletion of Scheduled visits
                if (visit.Status != "Scheduled")
                {
                    Console.WriteLine($"Cannot delete visit {visitId} with status '{visit.Status}' - only Scheduled visits can be deleted");
                    return false;
                }

                _db.Visits.Remove(visit);
                await _db.SaveChangesAsync();

                Console.WriteLine($"Visit {visitId} deleted successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting visit {visitId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get visits by enrollment ID
        /// </summary>
        public async Task<List<VisitDto>> GetByEnrollmentIdAsync(long enrollmentId)
        {
            try
            {
                var visits = await _db.Visits
                    .Where(v => v.EnrollmentID == enrollmentId)
                    .Include(v => v.Enrollment)
                        .ThenInclude(e => e.Patient)
                    .Include(v => v.Enrollment)
                        .ThenInclude(e => e.SiteProtocol)
                            .ThenInclude(sp => sp.Protocol)
                    .OrderBy(v => v.VisitDate)
                    .ToListAsync();

                return visits.Select(v => new VisitDto
                {
                    VisitID = v.VisitID,
                    EnrollmentID = v.EnrollmentID,
                    PatientName = v.Enrollment?.Patient?.Name ?? "Unknown",
                    ProtocolTitle = v.Enrollment?.SiteProtocol?.Protocol?.Title ?? "Unknown",
                    VisitDate = v.VisitDate,
                    Status = v.Status,
                    Notes = v.Notes
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching visits for enrollment {enrollmentId}: {ex.Message}");
                return new List<VisitDto>();
            }
        }

        /// <summary>
        /// Get upcoming visits (scheduled for future dates)
        /// </summary>
        public async Task<List<VisitDto>> GetUpcomingVisitsAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var visits = await _db.Visits
                    .Include(v => v.Enrollment)
                        .ThenInclude(e => e.Patient)
                    .Include(v => v.Enrollment)
                        .ThenInclude(e => e.SiteProtocol)
                            .ThenInclude(sp => sp.Protocol)
                    .Where(v => v.VisitDate >= today && v.Status == "Scheduled")
                    .OrderBy(v => v.VisitDate)
                    .ToListAsync();

                return visits.Select(v => new VisitDto
                {
                    VisitID = v.VisitID,
                    EnrollmentID = v.EnrollmentID,
                    PatientName = v.Enrollment?.Patient?.Name ?? "Unknown",
                    ProtocolTitle = v.Enrollment?.SiteProtocol?.Protocol?.Title ?? "Unknown",
                    VisitDate = v.VisitDate,
                    Status = v.Status,
                    Notes = v.Notes
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching upcoming visits: {ex.Message}");
                return new List<VisitDto>();
            }
        }

        /// <summary>
        /// Get completed visits
        /// </summary>
        public async Task<List<VisitDto>> GetCompletedVisitsAsync()
        {
            try
            {
                var visits = await _db.Visits
                    .Include(v => v.Enrollment)
                        .ThenInclude(e => e.Patient)
                    .Include(v => v.Enrollment)
                        .ThenInclude(e => e.SiteProtocol)
                            .ThenInclude(sp => sp.Protocol)
                    .Where(v => v.Status == "Completed")
                    .OrderByDescending(v => v.VisitDate)
                    .ToListAsync();

                return visits.Select(v => new VisitDto
                {
                    VisitID = v.VisitID,
                    EnrollmentID = v.EnrollmentID,
                    PatientName = v.Enrollment?.Patient?.Name ?? "Unknown",
                    ProtocolTitle = v.Enrollment?.SiteProtocol?.Protocol?.Title ?? "Unknown",
                    VisitDate = v.VisitDate,
                    Status = v.Status,
                    Notes = v.Notes
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching completed visits: {ex.Message}");
                return new List<VisitDto>();
            }
        }
    }
}