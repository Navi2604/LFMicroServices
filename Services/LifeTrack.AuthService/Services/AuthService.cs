// ============================================================
// AuthService.API / Services / AuthService.cs
// WITH SAFE AUDIT LOGGING (won't crash if audit fails)
// ============================================================

using LifeTrack.AuthService.DTOs;
using LifeTrack.AuthService.Repositories.Interfaces;
using LifeTrack.AuthService.Services.Interfaces;
using LifeTrack.Shared.Helpers;
using LifeTrack.Shared.Models;
using LifeTrack.Shared.Wrappers;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LifeTrack.AuthService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;
        private readonly AuditHttpClient? _audit;

        public AuthService(IAuthRepository repo, IConfiguration config, AuditHttpClient? audit = null)
        {
            _repo = repo;
            _config = config;
            _audit = audit;
        }

        public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest req)
        {
            var user = await _repo.GetUserByEmailAsync(req.Email);
            if (user == null)
                return ApiResponse<LoginResponse>.Fail("Invalid email or password.");

            if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return ApiResponse<LoginResponse>.Fail("Invalid email or password.");

            if (!user.IsActive)
                return ApiResponse<LoginResponse>.Fail("Your account has been deactivated. Contact admin.");

            var roleName = user.Role?.RoleName ?? "Unknown";
            var token = GenerateToken(user.UserID, user.Name, user.Email, roleName);
            var expiry = DateTime.UtcNow.AddHours(8);

            // ✅ LOG LOGIN AUDIT (safe - won't crash if audit service is down)
            try
            {
                _audit?.Log("LOGIN", "User", user.UserID,
                    $"User '{user.Name}' ({user.Email}) logged in as {roleName}.",
                    user.UserID);
            }
            catch { /* Ignore audit failures */ }

            return ApiResponse<LoginResponse>.Ok(new LoginResponse
            {
                Token = token,
                UserName = user.Name,
                Email = user.Email,
                Role = roleName,
                UserId = user.UserID,
                ExpiresAt = expiry.ToString("o")
            });
        }

        public async Task<ApiResponse<LoginResponse>> LoginPatientAsync(LoginRequest req)
        {
            var patient = await _repo.GetPatientByEmailAsync(req.Email);
            if (patient == null)
                return ApiResponse<LoginResponse>.Fail("Invalid email or password.");

            if (!BCrypt.Net.BCrypt.Verify(req.Password, patient.PasswordHash))
                return ApiResponse<LoginResponse>.Fail("Invalid email or password.");

            var token = GenerateToken(patient.PatientID, patient.Name, patient.Email, "Patient");
            var expiry = DateTime.UtcNow.AddHours(8);

            // ✅ LOG LOGIN AUDIT (safe)
            try
            {
                _audit?.Log("LOGIN", "Patient", patient.PatientID,
                    $"Patient '{patient.Name}' ({patient.Email}) logged in.",
                    patient.PatientID);
            }
            catch { /* Ignore audit failures */ }

            return ApiResponse<LoginResponse>.Ok(new LoginResponse
            {
                Token = token,
                UserName = patient.Name,
                Email = patient.Email,
                Role = "Patient",
                UserId = patient.PatientID,
                ExpiresAt = expiry.ToString("o")
            });
        }

        public async Task<ApiResponse<bool>> RegisterPatientAsync(RegisterPatientRequest req)
        {
            // ✅ VALIDATION 1: Email uniqueness
            var existing = await _repo.GetPatientByEmailAsync(req.Email);
            if (existing != null)
                return ApiResponse<bool>.Fail("Email already registered.");

            // ✅ VALIDATION 2: Password strength
            if (!IsPasswordStrong(req.Password))
            {
                return ApiResponse<bool>.Fail(
                    "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.");
            }

            // ✅ VALIDATION 3: Age validation
            var age = DateTime.UtcNow.Year - req.DOB.Year;
            if (req.DOB > DateTime.UtcNow.AddYears(-age)) age--;

            if (req.DOB > DateTime.UtcNow)
                return ApiResponse<bool>.Fail("Date of birth cannot be in the future.");

            if (age < 18)
                return ApiResponse<bool>.Fail("You must be at least 18 years old to register.");

            if (age > 120)
                return ApiResponse<bool>.Fail("Please enter a valid date of birth.");

            // ✅ All validations passed - create patient
            var patient = new Patient
            {
                Name = req.Name,
                Email = req.Email,
                DOB = req.DOB,
                ContactInfo = req.ContactInfo,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
            };

            await _repo.CreatePatientAsync(patient);

            // ✅ LOG REGISTRATION AUDIT (safe)
            try
            {
                _audit?.Log("CREATE", "Patient", patient.PatientID,
                    $"Patient '{patient.Name}' ({patient.Email}) registered.");
            }
            catch { /* Ignore audit failures */ }

            return ApiResponse<bool>.Ok(true, "Registration successful. You can now log in.");
        }

        public async Task<ApiResponse<bool>> CreateStaffAsync(CreateStaffRequest req)
        {
            // ✅ VALIDATION 1: Email uniqueness
            var existing = await _repo.GetUserByEmailAsync(req.Email);
            if (existing != null)
                return ApiResponse<bool>.Fail("Email already in use.");

            // ✅ VALIDATION 2: Valid role
            var role = await _repo.GetRoleByIdAsync(req.RoleID);
            if (role == null)
                return ApiResponse<bool>.Fail("Invalid role selected.");

            // ✅ VALIDATION 3: Password strength
            if (!IsPasswordStrong(req.Password))
            {
                return ApiResponse<bool>.Fail(
                    "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.");
            }

            // Investigators start inactive — activated when assigned to a site-protocol
            // All other staff start active
            bool startActive = role.RoleName != "Investigator";

            var user = new User
            {
                Name = req.Name,
                Email = req.Email,
                Phone = req.Phone,
                RoleID = req.RoleID,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                IsActive = startActive
            };

            await _repo.CreateUserAsync(user);

            // ✅ LOG USER CREATION AUDIT (safe)
            try
            {
                _audit?.Log("CREATE", "User", user.UserID,
                    $"Staff user '{user.Name}' ({user.Email}) created with role {role.RoleName}.");
            }
            catch { /* Ignore audit failures */ }

            return ApiResponse<bool>.Ok(true, $"Staff user '{req.Name}' created successfully.");
        }

        // ✅ PASSWORD STRENGTH VALIDATOR
        private bool IsPasswordStrong(string password)
        {
            if (password.Length < 8) return false;
            if (!password.Any(char.IsUpper)) return false;           // At least one uppercase
            if (!password.Any(char.IsLower)) return false;           // At least one lowercase  
            if (!password.Any(char.IsDigit)) return false;           // At least one digit
            if (!password.Any(ch => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(ch))) return false;  // Special char
            return true;
        }

        private string GenerateToken(long id, string name, string email, string role)
        {
            var key = _config["Jwt:Key"] ?? "LifeTrackSuperSecretKey2024!@#$%^&*()";
            var issuer = _config["Jwt:Issuer"] ?? "LifeTrack";
            var audience = _config["Jwt:Audience"] ?? "LifeTrack";

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,   id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Name,               name),
                new Claim(ClaimTypes.Role,               role),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}