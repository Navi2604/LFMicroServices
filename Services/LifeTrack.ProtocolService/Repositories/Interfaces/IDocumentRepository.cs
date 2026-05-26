// ============================================================
// ProtocolService.API / Repositories / Interfaces / IDocumentRepository.cs
// ============================================================

using LifeTrack.ProtocolService.DTOs;

namespace LifeTrack.ProtocolService.Repositories.Interfaces
{
    public interface IDocumentRepository
    {
        Task<List<DocumentDto>> GetAllAsync(DocumentFilterDto filter);
        Task<DocumentDto?> GetByIdAsync(long id);
        Task<DocumentDto> CreateAsync(CreateDocumentRequest req, long uploaderID);
        Task<bool> SubmitForReviewAsync(long id);
        Task<DocumentDto?> ReviewAsync(long id, ReviewDocumentRequest req, long reviewerID);
        Task<bool> DeleteAsync(long id);
        Task<bool> ArchiveAsync(long id);
    }
}