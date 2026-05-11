using LifeTrack.AuditService.Data;
using LifeTrack.AuditService.Models;
using LifeTrack.Shared.DTOs;
using LifeTrack.Shared.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeTrack.AuditService.Controllers
{
    [ApiController]
    [Route("api/audit")]
    public class AuditController : ControllerBase
    {
        private readonly AuditDbContext _db;

        public AuditController(AuditDbContext db)
            => _db = db;

        // POST /api/audit/log
        // Called by ALL other services
        // fire and forget — no auth needed
        [HttpPost("log")]
        [AllowAnonymous]
        public async Task<IActionResult> Log(
            [FromBody] AuditLogRequest req)
        {
            var log = new AuditLog
            {
                UserID = req.UserId,
                Action = req.Action,
                Timestamp = req.Timestamp
            };
            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
            return Ok();
        }

        // GET /api/audit/logs
        // Admin reads all logs
        [HttpGet("logs")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetLogs(
            [FromQuery] string? userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = _db.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(
                    a => a.UserID == userId);

            var total = await query.CountAsync();
            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                total,
                page,
                pageSize,
                logs
            }));
        }
    }
}