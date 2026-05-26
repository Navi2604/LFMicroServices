// ============================================================
// UserService.API / Repositories / Interfaces / IUserRepository.cs
// Both UpdateAsync and ToggleActiveAsync return (bool, string)
// so the service can pass messages back to the controller.
// ============================================================

using LifeTrack.UserService.DTOs;

namespace LifeTrack.UserService.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<List<UserDto>> GetAllAsync(UserFilterDto filter);
        Task<UserDto?> GetByIdAsync(long id);
        Task<(bool success, string message)> UpdateAsync(long id, UpdateUserRequest req);
        Task<(bool success, string message)> ToggleActiveAsync(long id);
    }
}