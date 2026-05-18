// ============================================================
// PatientService.API / Controllers / AdverseEventController.cs
// ============================================================

using LifeTrack.PatientService.DTOs;
using LifeTrack.PatientService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.PatientService.Controllers
{
    [ApiController]
    [Route("api/adverse-events")]
    [Authorize]
    public class AdverseEventController : ControllerBase
    {
        private readonly IAdverseEventService _service;

        public AdverseEventController(IAdverseEventService service) => _service = service;

        // GET /api/adverse-events?patientId=&protocolId=&severity=&status=
        [HttpGet]
        [Authorize(Roles = "Admin,ClinicalTrialManager,Investigator,RegulatoryOfficer,DataManager")]
        public async Task<IActionResult> GetAll([FromQuery] AdverseEventFilterDto filter)
            => Ok(await _service.GetAllAsync(filter));

        // POST /api/adverse-events
        [HttpPost]
        [Authorize(Roles = "Admin,Investigator,ClinicalTrialManager")]
        public async Task<IActionResult> Create([FromBody] CreateAdverseEventRequest req)
        {
            var result = await _service.CreateAsync(req);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // PATCH /api/adverse-events/{id}/status
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin,Investigator,RegulatoryOfficer")]
        public async Task<IActionResult> UpdateStatus(
            long id, [FromBody] UpdateAdverseEventStatusRequest req)
        {
            var result = await _service.UpdateStatusAsync(id, req.Status);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // DELETE /api/adverse-events/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}