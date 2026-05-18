// ============================================================
// UserService.API / Services / UserService.cs
// ============================================================

using LifeTrack.Shared.Helpers;
using LifeTrack.Shared.Wrappers;
using LifeTrack.UserService.DTOs;
using LifeTrack.UserService.Repositories.Interfaces;
using LifeTrack.UserService.Services.Interfaces;

namespace LifeTrack.UserService.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        private readonly AuditHttpClient _audit;

        public UserService(IUserRepository repo, AuditHttpClient audit)
        {
            _repo = repo;
            _audit = audit;
        }

        public async Task<ApiResponse<List<UserDto>>> GetAllAsync(UserFilterDto filter)
            => ApiResponse<List<UserDto>>.Ok(await _repo.GetAllAsync(filter));

        public async Task<ApiResponse<UserDto>> GetByIdAsync(long id)
        {
            var user = await _repo.GetByIdAsync(id);
            return user == null
                ? ApiResponse<UserDto>.Fail("User not found.")
                : ApiResponse<UserDto>.Ok(user);
        }

        public async Task<ApiResponse<UserDto>> UpdateAsync(long id, UpdateUserRequest req)
        {
            var updated = await _repo.UpdateAsync(id, req);
            if (!updated)
                return ApiResponse<UserDto>.Fail("User not found.");

            var user = await _repo.GetByIdAsync(id);

            _audit.Log("UPDATE", "User", id,
                $"User '{req.Name}' ({req.Email}) updated. Role: '{req.RoleID}'.");

            return ApiResponse<UserDto>.Ok(user!, "User updated successfully.");
        }

        public async Task<ApiResponse<UserDto>> ToggleActiveAsync(long id)
        {
            var toggled = await _repo.ToggleActiveAsync(id);
            if (!toggled)
                return ApiResponse<UserDto>.Fail("User not found.");

            var user = await _repo.GetByIdAsync(id);
            var msg = user!.IsActive ? "User activated." : "User deactivated.";

            _audit.Log("UPDATE", "User", id,
                $"User '{user.Name}' ({user.Email}) {(user.IsActive ? "activated" : "deactivated")}.");

            return ApiResponse<UserDto>.Ok(user, msg);
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            var existing = await _repo.GetByIdAsync(id);
            await _repo.DeleteAsync(id);

            _audit.Log("DELETE", "User", id,
                $"User '{existing?.Name}' ({existing?.Email}) deleted.");

            return ApiResponse<bool>.Ok(true, "User deleted successfully.");
        }
    }
}