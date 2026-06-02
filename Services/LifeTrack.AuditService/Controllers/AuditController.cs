// ============================================================
// AuditService.API / Controllers / AuditController.cs
// ============================================================

using LifeTrack.AuditService.DTOs;
using LifeTrack.AuditService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.AuditService.Controllers
{
    [ApiController]
    [Route("api/audit")]
    [Authorize]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _service;

        public AuditController(IAuditService service) => _service = service;

        // GET /api/audit/logs?userId=&action=&entityType=&fromDate=&toDate=&page=&pageSize=
        [HttpGet("logs")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetLogs([FromQuery] AuditFilterDto filter)
            => Ok(await _service.GetLogsAsync(filter));

        // POST /api/audit/log  — called internally by other services
        [HttpPost("log")]
        [AllowAnonymous]
        public async Task<IActionResult> Log([FromBody] CreateAuditLogRequest req)
        {
            var result = await _service.LogAsync(req);
            return Ok(result);
        }
    }
}