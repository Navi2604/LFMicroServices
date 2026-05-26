// ============================================================
// AuthService.API / Repositories / AuthRepository.cs
// WITH CACHING — Updated
// ============================================================

using LifeTrack.AuthService.Repositories.Interfaces;
using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LifeTrack.AuthService.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly LifeTrackDbContext _db;
        private readonly IMemoryCache _cache;
        private const string USER_EMAIL_CACHE_KEY = "user_email_{0}";
        private const string PATIENT_EMAIL_CACHE_KEY = "patient_email_{0}";
        private const string ROLE_CACHE_KEY = "role_{0}";
        private const int CACHE_DURATION_MINUTES = 60;

        public AuthRepository(LifeTrackDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            // ✅ CREATE CACHE KEY FROM EMAIL
            string cacheKey = string.Format(USER_EMAIL_CACHE_KEY, email.ToLower());

            // ✅ CHECK CACHE FIRST
            if (_cache.TryGetValue(cacheKey, out User? cachedUser))
                return cachedUser;

            var user = await _db.Users
                                .Include(u => u.Role)
                                .FirstOrDefaultAsync(u => u.Email == email);

            // ✅ STORE IN CACHE (even if null, to prevent repeated DB hits)
            if (user != null)
                _cache.Set(cacheKey, user, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            return user;
        }

        public async Task<Patient?> GetPatientByEmailAsync(string email)
        {
            // ✅ CREATE CACHE KEY FROM EMAIL
            string cacheKey = string.Format(PATIENT_EMAIL_CACHE_KEY, email.ToLower());

            // ✅ CHECK CACHE FIRST
            if (_cache.TryGetValue(cacheKey, out Patient? cachedPatient))
                return cachedPatient;

            var patient = await _db.Patients
                                   .FirstOrDefaultAsync(p => p.Email == email);

            // ✅ STORE IN CACHE (even if null)
            if (patient != null)
                _cache.Set(cacheKey, patient, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            return patient;
        }

        public async Task<Role?> GetRoleByIdAsync(int roleId)
        {
            // ✅ CREATE CACHE KEY FROM ROLE ID
            string cacheKey = string.Format(ROLE_CACHE_KEY, roleId);

            // ✅ CHECK CACHE FIRST
            if (_cache.TryGetValue(cacheKey, out Role? cachedRole))
                return cachedRole;

            var role = await _db.Roles.FindAsync(roleId);

            // ✅ STORE IN CACHE
            if (role != null)
                _cache.Set(cacheKey, role, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            return role;
        }

        public async Task<User> CreateUserAsync(User user)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // ✅ INVALIDATE CACHE - New user created
            InvalidateUserCache(user.Email);

            return user;
        }

        public async Task<Patient> CreatePatientAsync(Patient patient)
        {
            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();

            // ✅ INVALIDATE CACHE - New patient created
            InvalidatePatientCache(patient.Email);

            return patient;
        }

        public async Task SaveChangesAsync()
            => await _db.SaveChangesAsync();

        private void InvalidateUserCache(string email)
        {
            string cacheKey = string.Format(USER_EMAIL_CACHE_KEY, email.ToLower());
            _cache.Remove(cacheKey);
        }

        private void InvalidatePatientCache(string email)
        {
            string cacheKey = string.Format(PATIENT_EMAIL_CACHE_KEY, email.ToLower());
            _cache.Remove(cacheKey);
        }
    }
}