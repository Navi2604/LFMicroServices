// ============================================================
// SiteService.API / Controllers / SiteProtocolController.cs
// WITH DELETE — No changes (as designed)
// ============================================================

using LifeTrack.SiteService.DTOs;
using LifeTrack.SiteService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.SiteService.Controllers
{
    [ApiController]
    [Route("api/site-protocols")]
    [Authorize]
    public class SiteProtocolController : ControllerBase
    {
        private readonly ISiteProtocolService _service;

        public SiteProtocolController(ISiteProtocolService service) => _service = service;

        // GET /api/site-protocols?siteId=&protocolId=&status=
        [HttpGet]
        [Authorize(Roles = "Admin,ClinicalTrialManager,Investigator,RegulatoryOfficer,DataManager")]
        public async Task<IActionResult> GetAll([FromQuery] SiteProtocolFilterDto filter)
            => Ok(await _service.GetAllAsync(filter));

        // GET /api/site-protocols/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,ClinicalTrialManager,Investigator,RegulatoryOfficer,DataManager")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // POST /api/site-protocols
        [HttpPost]
        [Authorize(Roles = "Admin,ClinicalTrialManager")]
        public async Task<IActionResult> Create([FromBody] CreateSiteProtocolRequest req)
        {
            var result = await _service.CreateAsync(req);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // PATCH /api/site-protocols/{id}/status
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin,ClinicalTrialManager")]
        public async Task<IActionResult> UpdateStatus(
            long id, [FromBody] UpdateSiteProtocolStatusRequest req)
        {
            var result = await _service.UpdateStatusAsync(id, req.Status);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // DELETE /api/site-protocols/{id} — ✅ ALLOWED (unassigns investigator)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}