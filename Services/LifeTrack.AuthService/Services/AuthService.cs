using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LifeTrack.AuthService.DTOs;
using LifeTrack.AuthService.Services.Interfaces;
using LifeTrack.Shared.Data;
using LifeTrack.Shared.Models;
using LifeTrack.Shared.Wrappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LifeTrack.AuthService.Services
{
    public class AuthService : IAuthService
    {
        private readonly LifeTrackDbContext _db;
        private readonly IConfiguration _config;

        public AuthService(
            LifeTrackDbContext db,
            IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // ── LOGIN ─────────────────────────────────────────
        public async Task<ApiResponse<LoginResponse>> LoginAsync(
            LoginRequest req)
        {
            // Step 1 — Try staff login (User table)
            var user = await _db.Users
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() == req.Email.ToLower());

            if (user != null)
            {
                if (!user.IsActive)
                    return ApiResponse<LoginResponse>.Fail(
                        "Account deactivated. Contact admin.");

                if (!BCrypt.Net.BCrypt.Verify(
                    req.Password, user.PasswordHash))
                    return ApiResponse<LoginResponse>.Fail(
                        "Invalid email or password.");

                // Get role name from Role table
                // because User table has no RoleName column
                var role = await _db.Roles
                    .FindAsync(user.RoleID);
                var roleName = role?.RoleName ?? string.Empty;

                var token = GenerateToken(
                    user.UserID, user.Name,
                    user.Email, roleName);

                return ApiResponse<LoginResponse>.Ok(
                    new LoginResponse
                    {
                        Token = token,
                        UserName = user.Name,
                        Email = user.Email,
                        Role = roleName,
                        UserId = user.UserID,
                        ExpiresAt = DateTime.UtcNow.AddHours(8)
                    }, "Login successful.");
            }

            // Step 2 — Try patient login (Patient table)
            var patient = await _db.Patients
                .FirstOrDefaultAsync(p =>
                    p.Email != null &&
                    p.Email.ToLower() == req.Email.ToLower());

            if (patient == null)
                return ApiResponse<LoginResponse>.Fail(
                    "Invalid email or password.");

            if (patient.PasswordHash == null ||
                !BCrypt.Net.BCrypt.Verify(
                    req.Password, patient.PasswordHash))
                return ApiResponse<LoginResponse>.Fail(
                    "Invalid email or password.");

            var patientToken = GenerateToken(
                patient.PatientID, patient.Name,
                patient.Email ?? string.Empty, "Patient");

            return ApiResponse<LoginResponse>.Ok(
                new LoginResponse
                {
                    Token = patientToken,
                    UserName = patient.Name,
                    Email = patient.Email ?? string.Empty,
                    Role = "Patient",
                    UserId = patient.PatientID,
                    ExpiresAt = DateTime.UtcNow.AddHours(8)
                }, "Login successful.");
        }

        // ── REGISTER (Patient self-registration) ──────────
        public async Task<ApiResponse<UserDto>> RegisterAsync(
            RegisterRequest req)
        {
            // Check email not already used
            var userExists = await _db.Users.AnyAsync(
                u => u.Email.ToLower() == req.Email.ToLower());
            var patientExists = await _db.Patients.AnyAsync(
                p => p.Email != null &&
                     p.Email.ToLower() == req.Email.ToLower());

            if (userExists || patientExists)
                return ApiResponse<UserDto>.Fail(
                    "Email already in use.");

            var patient = new Patient
            {
                Name = req.Name,
                Email = req.Email,
                DOB = req.DOB,
                ContactInfo = req.ContactInfo,
                PasswordHash = BCrypt.Net.BCrypt
                    .HashPassword(req.Password),
                EnrollmentStatus = "Pending"
            };

            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();

            return ApiResponse<UserDto>.Ok(new UserDto
            {
                UserID = patient.PatientID,
                Name = patient.Name,
                Email = patient.Email ?? string.Empty,
                RoleName = "Patient",
                IsActive = true
            }, "Registration successful.");
        }

        // ── CREATE STAFF (Admin only) ──────────────────────
        public async Task<ApiResponse<UserDto>> CreateStaffAsync(
            CreateStaffRequest req)
        {
            var exists = await _db.Users.AnyAsync(
                u => u.Email.ToLower() == req.Email.ToLower());
            if (exists)
                return ApiResponse<UserDto>.Fail(
                    "Email already in use.");

            var user = new User
            {
                Name = req.Name,
                Email = req.Email,
                Phone = req.Phone,
                RoleID = req.RoleID,
                IsActive = true,
                PasswordHash = BCrypt.Net.BCrypt
                    .HashPassword(req.Password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return ApiResponse<UserDto>.Ok(new UserDto
            {
                UserID = user.UserID,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                RoleID = user.RoleID,
                RoleName = req.RoleName,
                IsActive = user.IsActive
            }, "Staff user created.");
        }

        // ── JWT TOKEN GENERATION ───────────────────────────
        private string GenerateToken(
            long id, string name,
            string email, string role)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _config["Jwt:Key"] ?? ""));
            var creds = new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier,
                    id.ToString()),
                new Claim(ClaimTypes.Name,  name),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role,  role),
                new Claim(JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}