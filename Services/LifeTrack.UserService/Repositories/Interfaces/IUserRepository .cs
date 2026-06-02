// ============================================================
// UserService.API / Repositories / Interfaces / IUserRepository.cs
// ============================================================

using LifeTrack.UserService.DTOs;

namespace LifeTrack.UserService.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<List<UserDto>> GetAllAsync(UserFilterDto filter);
        Task<UserDto?> GetByIdAsync(long id);
        Task<bool> UpdateAsync(long id, UpdateUserRequest req);
        Task<bool> ToggleActiveAsync(long id);
        Task<bool> DeleteAsync(long id);
    }
}