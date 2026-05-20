// ============================================================
// AuthService.API / Services / AuthService.cs
// ============================================================

using LifeTrack.AuthService.DTOs;
using LifeTrack.AuthService.Repositories.Interfaces;
using LifeTrack.AuthService.Services.Interfaces;
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

        public AuthService(IAuthRepository repo, IConfiguration config)
        {
            _repo = repo;
            _config = config;
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
            var existing = await _repo.GetPatientByEmailAsync(req.Email);
            if (existing != null)
                return ApiResponse<bool>.Fail("Email already registered.");

            var patient = new Patient
            {
                Name = req.Name,
                Email = req.Email,
                DOB = req.DOB,
                ContactInfo = req.ContactInfo,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
            };

            await _repo.CreatePatientAsync(patient);
            return ApiResponse<bool>.Ok(true, "Registration successful. You can now log in.");
        }

        public async Task<ApiResponse<bool>> CreateStaffAsync(CreateStaffRequest req)
        {
            var existing = await _repo.GetUserByEmailAsync(req.Email);
            if (existing != null)
                return ApiResponse<bool>.Fail("Email already in use.");

            var role = await _repo.GetRoleByIdAsync(req.RoleID);
            if (role == null)
                return ApiResponse<bool>.Fail("Invalid role selected.");

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
            return ApiResponse<bool>.Ok(true, $"Staff user '{req.Name}' created successfully.");
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