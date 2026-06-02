// ============================================================
// UserService.API / Controllers / UserController.cs
// ============================================================

using LifeTrack.Shared.Wrappers;
using LifeTrack.UserService.DTOs;
using LifeTrack.UserService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.UserService.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _service;

        public UserController(IUserService service) => _service = service;

        // GET /api/users?name=&email=&roleId=&isActive=
        [HttpGet]
        [Authorize(Roles = "Admin,ClinicalTrialManager")]
        public async Task<IActionResult> GetAll([FromQuery] UserFilterDto filter)
            => Ok(await _service.GetAllAsync(filter));

        // GET /api/users/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,ClinicalTrialManager")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // PUT /api/users/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateUserRequest req)
        {
            var result = await _service.UpdateAsync(id, req);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // PATCH /api/users/{id}/toggle
        [HttpPatch("{id}/toggle")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Toggle(long id)
        {
            var result = await _service.ToggleActiveAsync(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // DELETE /api/users/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}