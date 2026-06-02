// ============================================================
// AuthService.API / Repositories / AuthRepository.cs
// ============================================================

using LifeTrack.AuthService.Repositories.Interfaces;
using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.AuthService.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly LifeTrackDbContext _db;

        public AuthRepository(LifeTrackDbContext db) => _db = db;

        public async Task<User?> GetUserByEmailAsync(string email)
            => await _db.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Email == email);

        public async Task<Patient?> GetPatientByEmailAsync(string email)
            => await _db.Patients
                        .FirstOrDefaultAsync(p => p.Email == email);

        public async Task<Role?> GetRoleByIdAsync(int roleId)
            => await _db.Roles.FindAsync(roleId);

        public async Task<User> CreateUserAsync(User user)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        public async Task<Patient> CreatePatientAsync(Patient patient)
        {
            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();
            return patient;
        }

        public async Task SaveChangesAsync()
            => await _db.SaveChangesAsync();
    }
}