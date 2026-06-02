// ============================================================
// ProtocolService.API / Repositories / DocumentRepository.cs
// ============================================================

using LifeTrack.ProtocolService.DTOs;
using LifeTrack.ProtocolService.Repositories.Interfaces;
using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.ProtocolService.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly LifeTrackDbContext _db;

        public DocumentRepository(LifeTrackDbContext db) => _db = db;

        // ── GET ALL ──────────────────────────────────────────────

        public async Task<List<DocumentDto>> GetAllAsync(DocumentFilterDto filter)
        {
            var query = _db.Documents
                .Include(d => d.Protocol)
                .Include(d => d.Uploader)
                .Include(d => d.Reviewer)
                .AsQueryable();

            if (filter.ProtocolID.HasValue)
                query = query.Where(d => d.ProtocolID == filter.ProtocolID.Value);

            if (!string.IsNullOrEmpty(filter.Type))
                query = query.Where(d => d.Type == filter.Type);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(d => d.Status == filter.Status);

            return await query
                .OrderByDescending(d => d.UploadedAt)
                .Select(d => ToDto(d))
                .ToListAsync();
        }

        // ── GET BY ID ────────────────────────────────────────────

        public async Task<DocumentDto?> GetByIdAsync(long id)
        {
            var d = await _db.Documents
                .Include(d => d.Protocol)
                .Include(d => d.Uploader)
                .Include(d => d.Reviewer)
                .FirstOrDefaultAsync(d => d.DocumentID == id);

            return d == null ? null : ToDto(d);
        }

        // ── CREATE (status = Draft) ──────────────────────────────

        public async Task<DocumentDto> CreateAsync(CreateDocumentRequest req, long uploaderID)
        {
            var doc = new Document
            {
                ProtocolID = req.ProtocolID,
                UploadedBy = uploaderID,
                Title = req.Title,
                Type = req.Type,
                Version = req.Version,
                FileURL = req.FileURL,
                Status = "Draft",
                UploadedAt = DateTime.UtcNow
            };

            _db.Documents.Add(doc);
            await _db.SaveChangesAsync();

            return (await GetByIdAsync(doc.DocumentID))!;
        }

        // ── SUBMIT FOR REVIEW ────────────────────────────────────
        // Notify all RegulatoryOfficers (RoleID = 5)

        public async Task<bool> SubmitForReviewAsync(long id)
        {
            var doc = await _db.Documents
                .Include(d => d.Protocol)
                .FirstOrDefaultAsync(d => d.DocumentID == id);

            if (doc == null || doc.Status != "Draft") return false;

            doc.Status = "Under Review";

            // Notify all active RegulatoryOfficers
            var message = $"📄 Document \"{doc.Title}\" ({doc.Type} v{doc.Version}) " +
                          $"for protocol \"{doc.Protocol?.Title}\" is awaiting your review.";

            var roIds = await _db.Users
                .Where(u => u.RoleID == 5 && u.IsActive)
                .Select(u => u.UserID)
                .ToListAsync();

            foreach (var uid in roIds)
            {
                _db.Notifications.Add(new Notification
                {
                    UserID = uid,
                    Message = message,
                    Category = "Compliance",
                    Status = "Unread",
                    CreatedDate = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return true;
        }

        // ── REVIEW (Approve or Reject) ───────────────────────────
        // Approve → supersede old approved version of same Type+Protocol
        // Reject  → review notes required, notify uploader

        public async Task<DocumentDto?> ReviewAsync(
            long id, ReviewDocumentRequest req, long reviewerID)
        {
            var doc = await _db.Documents
                .Include(d => d.Protocol)
                .Include(d => d.Uploader)
                .FirstOrDefaultAsync(d => d.DocumentID == id);

            if (doc == null || doc.Status != "Under Review") return null;

            doc.ReviewedBy = reviewerID;
            doc.ReviewedAt = DateTime.UtcNow;
            doc.ReviewNotes = req.ReviewNotes;

            if (req.Approve)
            {
                doc.Status = "Approved";

                // Supersede any previously Approved version of same Type + Protocol
                var oldApproved = await _db.Documents
                    .Where(d => d.ProtocolID == doc.ProtocolID
                             && d.Type == doc.Type
                             && d.Status == "Approved"
                             && d.DocumentID != doc.DocumentID)
                    .ToListAsync();

                foreach (var old in oldApproved)
                    old.Status = "Superseded";

                // Notify uploader: document approved
                _db.Notifications.Add(new Notification
                {
                    UserID = doc.UploadedBy,
                    Message = $"✅ Your document \"{doc.Title}\" " +
                                  $"({doc.Type} v{doc.Version}) has been approved.",
                    Category = "Compliance",
                    Status = "Unread",
                    CreatedDate = DateTime.UtcNow
                });
            }
            else
            {
                doc.Status = "Rejected";

                // Notify uploader: document rejected with reason
                var reason = string.IsNullOrEmpty(req.ReviewNotes)
                    ? "No reason provided."
                    : req.ReviewNotes;

                _db.Notifications.Add(new Notification
                {
                    UserID = doc.UploadedBy,
                    Message = $"❌ Your document \"{doc.Title}\" " +
                                  $"({doc.Type} v{doc.Version}) was rejected. " +
                                  $"Reason: {reason}",
                    Category = "Compliance",
                    Status = "Unread",
                    CreatedDate = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return await GetByIdAsync(doc.DocumentID);
        }

        // ── DELETE (Draft or Rejected only) ─────────────────────

        public async Task<bool> DeleteAsync(long id)
        {
            var doc = await _db.Documents.FindAsync(id);
            if (doc == null) return false;
            if (doc.Status != "Draft" && doc.Status != "Rejected") return false;

            _db.Documents.Remove(doc);
            await _db.SaveChangesAsync();
            return true;
        }

        // ── ARCHIVE (Rejected only, Admin only) ──────────────────────
        // Sets status to Archived — soft removal, preserves audit trail

        public async Task<bool> ArchiveAsync(long id)
        {
            var doc = await _db.Documents.FindAsync(id);

            // Only Rejected documents can be archived
            if (doc == null || doc.Status != "Rejected") return false;

            doc.Status = "Archived";
            await _db.SaveChangesAsync();
            return true;
        }

        // ── Private DTO mapper ───────────────────────────────────

        private static DocumentDto ToDto(Document d) => new()
        {
            DocumentID = d.DocumentID,
            ProtocolID = d.ProtocolID,
            ProtocolTitle = d.Protocol?.Title ?? string.Empty,
            UploadedBy = d.UploadedBy,
            UploaderName = d.Uploader?.Name ?? string.Empty,
            Title = d.Title,
            Type = d.Type,
            Version = d.Version,
            FileURL = d.FileURL,
            Status = d.Status,
            UploadedAt = d.UploadedAt,
            ReviewNotes = d.ReviewNotes,
            ReviewedBy = d.ReviewedBy,
            ReviewerName = d.Reviewer?.Name ?? null,
            ReviewedAt = d.ReviewedAt
        };
    }
}