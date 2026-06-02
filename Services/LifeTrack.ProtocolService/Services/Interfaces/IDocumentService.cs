// ============================================================
// ProtocolService.API / Services / Interfaces / IDocumentService.cs
// ============================================================

using LifeTrack.ProtocolService.DTOs;
using LifeTrack.Shared.Wrappers;

namespace LifeTrack.ProtocolService.Services.Interfaces
{
    public interface IDocumentService
    {
        Task<ApiResponse<List<DocumentDto>>> GetAllAsync(DocumentFilterDto filter);
        Task<ApiResponse<DocumentDto>> GetByIdAsync(long id);
        Task<ApiResponse<DocumentDto>> CreateAsync(CreateDocumentRequest req, long uploaderID);
        Task<ApiResponse<bool>> SubmitForReviewAsync(long id);
        Task<ApiResponse<DocumentDto>> ReviewAsync(long id, ReviewDocumentRequest req, long reviewerID);
        Task<ApiResponse<bool>> DeleteAsync(long id);
    }
}