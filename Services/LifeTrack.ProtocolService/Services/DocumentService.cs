// ============================================================
// ProtocolService.API / Services / DocumentService.cs
// ============================================================

using LifeTrack.ProtocolService.DTOs;
using LifeTrack.ProtocolService.Repositories.Interfaces;
using LifeTrack.ProtocolService.Services.Interfaces;
using LifeTrack.Shared.Helpers;
using LifeTrack.Shared.Wrappers;

namespace LifeTrack.ProtocolService.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _repo;
        private readonly AuditHttpClient _audit;

        public DocumentService(IDocumentRepository repo, AuditHttpClient audit)
        {
            _repo = repo;
            _audit = audit;
        }

        public async Task<ApiResponse<List<DocumentDto>>> GetAllAsync(DocumentFilterDto filter)
            => ApiResponse<List<DocumentDto>>.Ok(await _repo.GetAllAsync(filter));

        public async Task<ApiResponse<DocumentDto>> GetByIdAsync(long id)
        {
            var doc = await _repo.GetByIdAsync(id);
            return doc == null
                ? ApiResponse<DocumentDto>.Fail("Document not found.")
                : ApiResponse<DocumentDto>.Ok(doc);
        }

        public async Task<ApiResponse<DocumentDto>> CreateAsync(
            CreateDocumentRequest req, long uploaderID)
        {
            var doc = await _repo.CreateAsync(req, uploaderID);

            _audit.Log("CREATE", "Document", doc.DocumentID,
                $"Document '{doc.Title}' ({doc.Type} v{doc.Version}) " +
                $"uploaded for protocol '{doc.ProtocolTitle}'. Status: Draft.");

            return ApiResponse<DocumentDto>.Ok(doc, "Document created successfully.");
        }

        public async Task<ApiResponse<bool>> SubmitForReviewAsync(long id)
        {
            var submitted = await _repo.SubmitForReviewAsync(id);

            if (!submitted)
                return ApiResponse<bool>.Fail(
                    "Document not found or is not in Draft status.");

            _audit.Log("UPDATE", "Document", id,
                "Document submitted for regulatory review. Status: Under Review.");

            return ApiResponse<bool>.Ok(true,
                "Document submitted for review. Regulatory Officers have been notified.");
        }

        public async Task<ApiResponse<DocumentDto>> ReviewAsync(
            long id, ReviewDocumentRequest req, long reviewerID)
        {
            if (!req.Approve && string.IsNullOrWhiteSpace(req.ReviewNotes))
                return ApiResponse<DocumentDto>.Fail(
                    "Review notes are required when rejecting a document.");

            var doc = await _repo.ReviewAsync(id, req, reviewerID);

            if (doc == null)
                return ApiResponse<DocumentDto>.Fail(
                    "Document not found or is not in Under Review status.");

            var action = req.Approve ? "Approved" : "Rejected";
            _audit.Log("UPDATE", "Document", id,
                $"Document '{doc.Title}' {action} by reviewer ID {reviewerID}. " +
                (req.ReviewNotes != null ? $"Notes: {req.ReviewNotes}" : ""));

            return ApiResponse<DocumentDto>.Ok(doc,
                $"Document {action.ToLower()} successfully.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            var deleted = await _repo.DeleteAsync(id);

            if (!deleted)
                return ApiResponse<bool>.Fail(
                    "Document not found or cannot be deleted " +
                    "(only Draft and Rejected documents can be deleted).");

            _audit.Log("DELETE", "Document", id,
                $"Document ID {id} deleted.");

            return ApiResponse<bool>.Ok(true, "Document deleted.");
        }

        public async Task<ApiResponse<bool>> ArchiveAsync(long id)
        {
            var archived = await _repo.ArchiveAsync(id);

            if (!archived)
                return ApiResponse<bool>.Fail(
                    "Document not found or is not in Rejected status. " +
                    "Only Rejected documents can be archived.");

            _audit.Log("UPDATE", "Document", id,
                "Document archived by Admin. Status: Archived.");

            return ApiResponse<bool>.Ok(true, "Document archived successfully.");
        }
    }
}