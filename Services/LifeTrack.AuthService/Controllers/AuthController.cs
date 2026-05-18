// ============================================================
// AuthService.API / Controllers / AuthController.cs
// ============================================================

using LifeTrack.AuthService.DTOs;
using LifeTrack.AuthService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.AuthService.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _service;

        public AuthController(IAuthService service) => _service = service;

        // POST /api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var result = await _service.LoginAsync(req);
            return result.Success ? Ok(result) : Unauthorized(result);
        }

        // POST /api/auth/login-patient
        [HttpPost("login-patient")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginPatient([FromBody] LoginRequest req)
        {
            var result = await _service.LoginPatientAsync(req);
            return result.Success ? Ok(result) : Unauthorized(result);
        }

        // POST /api/auth/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterPatientRequest req)
        {
            var result = await _service.RegisterPatientAsync(req);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // POST /api/auth/create-staff
        [HttpPost("create-staff")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest req)
        {
            var result = await _service.CreateStaffAsync(req);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}