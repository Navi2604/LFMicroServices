// ============================================================
// SiteService.API / Controllers / DeviationController.cs
// ============================================================

using LifeTrack.SiteService.DTOs;
using LifeTrack.SiteService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.SiteService.Controllers
{
    [ApiController]
    [Route("api/deviations")]
    [Authorize]
    public class DeviationController : ControllerBase
    {
        private readonly IDeviationService _service;

        public DeviationController(IDeviationService service) => _service = service;

        // GET /api/deviations?siteProtocolId=&severity=&status=
        [HttpGet]
        [Authorize(Roles = "Admin,ClinicalTrialManager,Investigator,RegulatoryOfficer,DataManager")]
        public async Task<IActionResult> GetAll([FromQuery] DeviationFilterDto filter)
            => Ok(await _service.GetAllAsync(filter));

        // GET /api/deviations/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,ClinicalTrialManager,Investigator,RegulatoryOfficer,DataManager")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // POST /api/deviations
        [HttpPost]
        [Authorize(Roles = "Admin,Investigator,ClinicalTrialManager")]
        public async Task<IActionResult> Create([FromBody] CreateDeviationRequest req)
        {
            var result = await _service.CreateAsync(req);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // PATCH /api/deviations/{id}/status
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "ClinicalTrialManager,RegulatoryOfficer")]
        public async Task<IActionResult> UpdateStatus(
            long id, [FromBody] UpdateDeviationStatusRequest req)
        {
            var updaterRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";

            var result = await _service.UpdateStatusAsync(id, req.Status, updaterRole);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // DELETE /api/deviations/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}