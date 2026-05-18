// ============================================================
// AuthService.API / Repositories / Interfaces / IAuthRepository.cs
// ============================================================

using LifeTrack.Shared.Models;

namespace LifeTrack.AuthService.Repositories.Interfaces
{
    public interface IAuthRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<Patient?> GetPatientByEmailAsync(string email);
        Task<Role?> GetRoleByIdAsync(int roleId);
        Task<User> CreateUserAsync(User user);
        Task<Patient> CreatePatientAsync(Patient patient);
        Task SaveChangesAsync();
    }
}