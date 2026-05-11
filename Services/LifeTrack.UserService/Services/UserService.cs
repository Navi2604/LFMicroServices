using LifeTrack.Shared.Data;
using LifeTrack.Shared.Wrappers;
using LifeTrack.UserService.DTOs;
using LifeTrack.UserService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.UserService.Services
{
    public class UserService : IUserService
    {
        private readonly LifeTrackDbContext _db;
        public UserService(LifeTrackDbContext db) => _db = db;

        public async Task<ApiResponse<List<UserDto>>> GetAllAsync()
        {
            var users = await _db.Users
                .Where(u => u.RoleID != 4)
                .ToListAsync();

            var roles = await _db.Roles.ToListAsync();
            var roleMap = roles.ToDictionary(
                r => r.RoleID, r => r.RoleName);

            var dtos = users.Select(u => new UserDto
            {
                UserID = u.UserID,
                Name = u.Name,
                Email = u.Email,
                Phone = u.Phone,
                RoleID = u.RoleID,
                RoleName = roleMap.TryGetValue(
                    u.RoleID, out var rn) ? rn : "Unknown",
                IsActive = u.IsActive
            }).ToList();

            return ApiResponse<List<UserDto>>.Ok(dtos);
        }

        public async Task<ApiResponse<UserDto>> GetByIdAsync(
            long id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return ApiResponse<UserDto>.Fail(
                    "User not found.");

            var role = await _db.Roles.FindAsync(user.RoleID);

            return ApiResponse<UserDto>.Ok(new UserDto
            {
                UserID = user.UserID,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                RoleID = user.RoleID,
                RoleName = role?.RoleName ?? "Unknown",
                IsActive = user.IsActive
            });
        }

        public async Task<ApiResponse<UserDto>> UpdateAsync(
            long id, UpdateUserRequest req)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return ApiResponse<UserDto>.Fail(
                    "User not found.");

            user.Name = req.Name;
            user.Email = req.Email;
            user.Phone = req.Phone;
            user.RoleID = req.RoleID;

            await _db.SaveChangesAsync();

            var role = await _db.Roles.FindAsync(user.RoleID);

            return ApiResponse<UserDto>.Ok(new UserDto
            {
                UserID = user.UserID,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                RoleID = user.RoleID,
                RoleName = role?.RoleName ?? "Unknown",
                IsActive = user.IsActive
            }, "User updated.");
        }

        public async Task<ApiResponse<UserDto>> ToggleActiveAsync(
            long id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return ApiResponse<UserDto>.Fail(
                    "User not found.");

            user.IsActive = !user.IsActive;
            await _db.SaveChangesAsync();

            var role = await _db.Roles.FindAsync(user.RoleID);

            return ApiResponse<UserDto>.Ok(new UserDto
            {
                UserID = user.UserID,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                RoleID = user.RoleID,
                RoleName = role?.RoleName ?? "Unknown",
                IsActive = user.IsActive
            }, user.IsActive ? "User activated."
                             : "User deactivated.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(long id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return ApiResponse<bool>.Fail(
                    "User not found.");

            if (user.IsActive)
                return ApiResponse<bool>.Fail(
                    "Deactivate user first before deleting.");

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "User deleted.");
        }
    }
}