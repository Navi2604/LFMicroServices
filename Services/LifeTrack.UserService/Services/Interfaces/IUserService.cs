// ============================================================
// UserService.API / Services / Interfaces / IUserService.cs
// ============================================================

using LifeTrack.Shared.Wrappers;
using LifeTrack.UserService.DTOs;

namespace LifeTrack.UserService.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse<List<UserDto>>> GetAllAsync(UserFilterDto filter);
        Task<ApiResponse<UserDto>> GetByIdAsync(long id);
        Task<ApiResponse<UserDto>> UpdateAsync(long id, UpdateUserRequest req);
        Task<ApiResponse<UserDto>> ToggleActiveAsync(long id);
        Task<ApiResponse<bool>> DeleteAsync(long id);
    }
}