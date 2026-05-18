// ============================================================
// UserService.API / Controllers / NotificationController.cs
// ============================================================

using LifeTrack.UserService.DTOs;
using LifeTrack.UserService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.UserService.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _service;

        public NotificationController(INotificationService service) => _service = service;

        // GET /api/notifications?userId=&status=&category=
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] NotificationFilterDto filter)
            => Ok(await _service.GetAllAsync(filter));

        // GET /api/notifications/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // POST /api/notifications  — internal use (other services call this)
        [HttpPost]
        [Authorize(Roles = "Admin,ClinicalTrialManager")]
        public async Task<IActionResult> Create([FromBody] CreateNotificationRequest req)
        {
            var result = await _service.CreateAsync(req);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // PATCH /api/notifications/{id}/read
        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            var result = await _service.MarkAsReadAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // PATCH /api/notifications/read-all/{userId}
        [HttpPatch("read-all/{userId}")]
        public async Task<IActionResult> MarkAllAsRead(long userId)
            => Ok(await _service.MarkAllAsReadAsync(userId));

        // DELETE /api/notifications/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}