// ============================================================
// ProtocolService.API / Controllers / ProtocolController.cs
// ADDED: PATCH /api/protocols/{id}/unarchive
// FIXED: Archive allowed from any non-Archived status
// ============================================================

using LifeTrack.ProtocolService.DTOs;
using LifeTrack.ProtocolService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.ProtocolService.Controllers
{
    [ApiController]
    [Route("api/protocols")]
    [Authorize]
    public class ProtocolController : ControllerBase
    {
        private readonly IProtocolService _service;

        public ProtocolController(IProtocolService service) => _service = service;

        // GET /api/protocols
        [HttpGet]
        [Authorize(Roles = "Admin,ClinicalTrialManager,Investigator,RegulatoryOfficer,DataManager")]
        public async Task<IActionResult> GetAll([FromQuery] ProtocolFilterDto filter)
            => Ok(await _service.GetAllAsync(filter));

        // GET /api/protocols/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,ClinicalTrialManager,Investigator,RegulatoryOfficer,DataManager")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // POST /api/protocols
        [HttpPost]
        [Authorize(Roles = "Admin,ClinicalTrialManager")]
        public async Task<IActionResult> Create([FromBody] CreateProtocolRequest req)
        {
            var result = await _service.CreateAsync(req);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // PUT /api/protocols/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ClinicalTrialManager")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateProtocolRequest req)
        {
            var result = await _service.UpdateAsync(id, req);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // PATCH /api/protocols/{id}/archive
        // Archives a protocol (moves to Archived status).
        // Allowed from any non-Archived status.
        [HttpPatch("{id}/archive")]
        [Authorize(Roles = "Admin,ClinicalTrialManager")]
        public async Task<IActionResult> Archive(long id)
        {
            var result = await _service.ArchiveAsync(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // PATCH /api/protocols/{id}/unarchive
        // Restores an Archived protocol back to its computed status
        // (Upcoming / Ongoing / Completed) based on start/end dates.
        [HttpPatch("{id}/unarchive")]
        [Authorize(Roles = "Admin,ClinicalTrialManager")]
        public async Task<IActionResult> Unarchive(long id)
        {
            var result = await _service.UnarchiveAsync(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // DELETE /api/protocols/{id} — Archived protocols only
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,ClinicalTrialManager")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}