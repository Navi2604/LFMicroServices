// ============================================================
// UserService.API / Repositories / UserRepository.cs
//
// RULES enforced here:
// 1. If saved role IS Investigator (roleID=3) → ALWAYS force IsActive=false,
//    regardless of what it was before. No exceptions. No loopholes.
//    Investigators only become Active via SiteProtocol assignment.
// 2. If saved role is NOT Investigator → do NOT touch IsActive.
//    (toggle endpoint handles activation/deactivation for other roles)
// 3. ToggleActiveAsync: hard-blocked for Investigators.
// ============================================================

using LifeTrack.Shared.Data;
using LifeTrack.UserService.DTOs;
using LifeTrack.UserService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LifeTrack.UserService.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly LifeTrackDbContext _db;
        private readonly IMemoryCache _cache;

        private const string LIST_KEY = "users_list_{0}_{1}_{2}_{3}";
        private const string SINGLE_KEY = "user_{0}";
        private const int TTL = 30; // minutes

        public UserRepository(LifeTrackDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        // ── GetAll ────────────────────────────────────────────────
        public async Task<List<UserDto>> GetAllAsync(UserFilterDto filter)
        {
            var key = string.Format(LIST_KEY,
                filter.Name ?? "null",
                filter.Email ?? "null",
                filter.RoleID?.ToString() ?? "null",
                filter.IsActive?.ToString() ?? "null");

            if (_cache.TryGetValue(key, out List<UserDto>? hit)) return hit!;

            var q = _db.Users.Include(u => u.Role)
                             .Where(u => u.RoleID != 4)   // exclude Patient role
                             .AsQueryable();

            if (!string.IsNullOrEmpty(filter.Name))
                q = q.Where(u => u.Name.Contains(filter.Name));
            if (!string.IsNullOrEmpty(filter.Email))
                q = q.Where(u => u.Email.Contains(filter.Email));
            if (filter.RoleID.HasValue)
                q = q.Where(u => u.RoleID == filter.RoleID.Value);
            if (filter.IsActive.HasValue)
                q = q.Where(u => u.IsActive == filter.IsActive.Value);

            var list = await q.OrderByDescending(u => u.UserID)
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

            _cache.Set(key, list, TimeSpan.FromMinutes(TTL));
            return list;
        }

        // ── GetById ───────────────────────────────────────────────
        public async Task<UserDto?> GetByIdAsync(long id)
        {
            var key = string.Format(SINGLE_KEY, id);
            if (_cache.TryGetValue(key, out UserDto? hit)) return hit;

            var u = await _db.Users.Include(x => x.Role)
                                   .FirstOrDefaultAsync(x => x.UserID == id);
            if (u == null) return null;

            var dto = new UserDto
            {
                UserID = u.UserID,
                Name = u.Name,
                Email = u.Email,
                Phone = u.Phone,
                RoleID = u.RoleID,
                RoleName = u.Role?.RoleName ?? "Unknown",
                IsActive = u.IsActive
            };

            _cache.Set(key, dto, TimeSpan.FromMinutes(TTL));
            return dto;
        }

        // ── UpdateAsync ───────────────────────────────────────────
        // KEY RULE: if the role being saved IS Investigator (3),
        // ALWAYS set IsActive = false — no matter what role it was before.
        // This closes every loophole: role-swap trick, direct edit, etc.
        public async Task<(bool success, string message)> UpdateAsync(
            long id, UpdateUserRequest req)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return (false, "User not found.");

            user.Name = req.Name;
            user.Email = req.Email;
            user.Phone = req.Phone;
            user.RoleID = req.RoleID;

            // ── The single absolute rule ──────────────────────────
            if (req.RoleID == 3)
            {
                // Role IS Investigator → always inactive.
                // SiteProtocol assignment is the only activation path.
                user.IsActive = false;
            }
            // For all other roles: do NOT touch IsActive here.
            // Activation/deactivation of non-Investigators is done
            // via the /toggle endpoint exclusively.

            await _db.SaveChangesAsync();
            BustCache(id);

            var roleName = req.RoleID == 3 ? "Investigator" : "non-Investigator";
            var note = req.RoleID == 3
                ? " (set Inactive — will activate automatically when assigned to a protocol)"
                : string.Empty;

            return (true, $"User updated successfully{note}.");
        }

        // ── ToggleActiveAsync ─────────────────────────────────────
        // Hard-blocked for Investigators. Free for all other roles.
        public async Task<(bool success, string message)> ToggleActiveAsync(long id)
        {
            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserID == id);

            if (user == null) return (false, "User not found.");

            if (user.RoleID == 3 || user.Role?.RoleName == "Investigator")
                return (false,
                    "Investigator status cannot be changed manually. " +
                    "It is controlled automatically by protocol site assignments.");

            user.IsActive = !user.IsActive;
            await _db.SaveChangesAsync();
            BustCache(id);

            return (true, user.IsActive
                ? $"'{user.Name}' activated successfully."
                : $"'{user.Name}' deactivated successfully.");
        }

        // ── Cache invalidation ────────────────────────────────────
        private void BustCache(long userId)
        {
            _cache.Remove(string.Format(SINGLE_KEY, userId));

            // Clear all list-cache permutations
            foreach (var a in new[] { "null", "True", "False" })
                foreach (var r in new[] { "null", "1", "2", "3", "5", "6" })
                    _cache.Remove(string.Format(LIST_KEY, "null", "null", r, a));
        }
    }
}