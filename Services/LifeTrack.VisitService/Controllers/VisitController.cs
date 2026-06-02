// ============================================================
// VisitService.API / Controllers / VisitController.cs
// ============================================================

using LifeTrack.VisitService.DTOs;
using LifeTrack.VisitService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.VisitService.Controllers
{
    [ApiController]
    [Route("api/visits")]
    [Authorize]
    public class VisitController : ControllerBase
    {
        private readonly IVisitService _service;

        public VisitController(IVisitService service) => _service = service;

        // GET /api/visits?enrollmentId=&status=&fromDate=&toDate=
        [HttpGet]
        [Authorize(Roles = "Admin,ClinicalTrialManager,Investigator,DataManager,RegulatoryOfficer")]
        public async Task<IActionResult> GetAll([FromQuery] VisitFilterDto filter)
            => Ok(await _service.GetAllAsync(filter));

        // GET /api/visits/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,ClinicalTrialManager,Investigator,DataManager,RegulatoryOfficer")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // POST /api/visits
        [HttpPost]
        [Authorize(Roles = "Admin,Investigator,ClinicalTrialManager")]
        public async Task<IActionResult> Create([FromBody] CreateVisitRequest req)
        {
            var result = await _service.CreateAsync(req);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // PATCH /api/visits/{id}/status
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin,Investigator")]
        public async Task<IActionResult> UpdateStatus(
            long id, [FromBody] UpdateVisitStatusRequest req)
        {
            var result = await _service.UpdateStatusAsync(id, req.Status);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // DELETE /api/visits/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}