// ============================================================
// UserService.API / Repositories / UserRepository.cs
// ============================================================

using LifeTrack.Shared.Data;
using LifeTrack.UserService.DTOs;
using LifeTrack.UserService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.UserService.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly LifeTrackDbContext _db;

        public UserRepository(LifeTrackDbContext db) => _db = db;

        public async Task<List<UserDto>> GetAllAsync(UserFilterDto filter)
        {
            var query = _db.Users
                           .Include(u => u.Role)
                           .Where(u => u.RoleID != 4) // exclude patients
                           .AsQueryable();

            if (!string.IsNullOrEmpty(filter.Name))
                query = query.Where(u => u.Name.Contains(filter.Name));

            if (!string.IsNullOrEmpty(filter.Email))
                query = query.Where(u => u.Email.Contains(filter.Email));

            if (filter.RoleID.HasValue)
                query = query.Where(u => u.RoleID == filter.RoleID.Value);

            if (filter.IsActive.HasValue)
                query = query.Where(u => u.IsActive == filter.IsActive.Value);

            return await query
                .OrderByDescending(u => u.UserID)
                .Select(u => new UserDto
                {
                    UserID = u.UserID,
                    Name = u.Name,
                    Email = u.Email,
                    Phone = u.Phone,
                    RoleID = u.RoleID,
                    RoleName = u.Role != null ? u.Role.RoleName : "Unknown",
                    IsActive = u.IsActive
                })
                .ToListAsync();
        }

        public async Task<UserDto?> GetByIdAsync(long id)
        {
            var u = await _db.Users
                             .Include(x => x.Role)
                             .FirstOrDefaultAsync(x => x.UserID == id);
            if (u == null) return null;

            return new UserDto
            {
                UserID = u.UserID,
                Name = u.Name,
                Email = u.Email,
                Phone = u.Phone,
                RoleID = u.RoleID,
                RoleName = u.Role?.RoleName ?? "Unknown",
                IsActive = u.IsActive
            };
        }

        public async Task<bool> UpdateAsync(long id, UpdateUserRequest req)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return false;

            user.Name = req.Name;
            user.Email = req.Email;
            user.Phone = req.Phone;
            user.RoleID = req.RoleID;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleActiveAsync(long id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return false;

            user.IsActive = !user.IsActive;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return false;

            if (user.IsActive)
                throw new InvalidOperationException(
                    "Deactivate the user before deleting.");

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}