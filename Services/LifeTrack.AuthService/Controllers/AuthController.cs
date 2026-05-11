using LifeTrack.AuthService.DTOs;
using LifeTrack.AuthService.Services.Interfaces;
using LifeTrack.Shared.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.AuthService.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
            => _authService = authService;

        // POST /api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest req)
        {
            var result = await _authService.LoginAsync(req);
            return result.Success
                ? Ok(result)
                : Unauthorized(result);
        }

        // POST /api/auth/register (patient)
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Password)
                || req.Password.Length < 8)
                return BadRequest(
                    ApiResponse<object>.Fail(
                        "Password must be at least 8 characters."));

            var result = await _authService.RegisterAsync(req);
            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        // POST /api/auth/create-staff (Admin only)
        [HttpPost("create-staff")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateStaff(
            [FromBody] CreateStaffRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Password)
                || req.Password.Length < 8)
                return BadRequest(
                    ApiResponse<object>.Fail(
                        "Password must be at least 8 characters."));

            var result = await _authService
                .CreateStaffAsync(req);
            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        // GET /api/auth/me
        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var id = User.FindFirst(
                System.Security.Claims.ClaimTypes
                    .NameIdentifier)?.Value;
            var name = User.FindFirst(
                System.Security.Claims.ClaimTypes
                    .Name)?.Value;
            var email = User.FindFirst(
                System.Security.Claims.ClaimTypes
                    .Email)?.Value;
            var role = User.FindFirst(
                System.Security.Claims.ClaimTypes
                    .Role)?.Value;

            return Ok(ApiResponse<object>.Ok(
                new { id, name, email, role }));
        }
    }
}