using LifeTrack.Shared.Wrappers;
using LifeTrack.VisitService.DTOs;

namespace LifeTrack.VisitService.Services.Interfaces
{
    public interface IVisitService
    {
        Task<ApiResponse<List<VisitDto>>> GetAllAsync();
        Task<ApiResponse<List<VisitDto>>> GetByPatientAsync(
            long patientId);
        Task<ApiResponse<VisitDto>> GetByIdAsync(long id);
        Task<ApiResponse<VisitDto>> CreateAsync(
            CreateVisitRequest req);
        Task<ApiResponse<VisitDto>> UpdateAsync(
            long id, CreateVisitRequest req);
        Task<ApiResponse<bool>> DeleteAsync(long id);
    }
}