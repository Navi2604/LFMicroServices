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
            var (success, message) = await _repo.UpdateAsync(id, req);
            if (!success)
                return ApiResponse<UserDto>.Fail(message);

            var user = await _repo.GetByIdAsync(id);
            _audit.Log("UPDATE", "User", id,
                $"User '{req.Name}' ({req.Email}) updated. RoleID: {req.RoleID}.");

            return ApiResponse<UserDto>.Ok(user!, message);
        }

        public async Task<ApiResponse<UserDto>> ToggleActiveAsync(long id)
        {
            var (success, message) = await _repo.ToggleActiveAsync(id);
            if (!success)
                return ApiResponse<UserDto>.Fail(message);

            var user = await _repo.GetByIdAsync(id);
            _audit.Log("UPDATE", "User", id,
                $"User '{user!.Name}' — {message}");

            return ApiResponse<UserDto>.Ok(user, message);
        }
    }
}