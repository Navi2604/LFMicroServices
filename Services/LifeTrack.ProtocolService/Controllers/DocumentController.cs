// ============================================================
// ProtocolService.API / Controllers / DocumentController.cs
// ============================================================

using LifeTrack.ProtocolService.DTOs;
using LifeTrack.ProtocolService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LifeTrack.ProtocolService.Controllers
{
    [ApiController]
    [Route("api/documents")]
    [Authorize]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _service;

        public DocumentController(IDocumentService service) => _service = service;

        // GET /api/documents?protocolId=&type=&status=
        [HttpGet]
        [Authorize(Roles = "Admin,ClinicalTrialManager,RegulatoryOfficer,DataManager,Investigator")]
        public async Task<IActionResult> GetAll([FromQuery] DocumentFilterDto filter)
            => Ok(await _service.GetAllAsync(filter));

        // GET /api/documents/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,ClinicalTrialManager,RegulatoryOfficer,DataManager,Investigator")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // POST /api/documents  — Admin and CTM upload
        [HttpPost]
        [Authorize(Roles = "Admin,ClinicalTrialManager")]
        public async Task<IActionResult> Create([FromBody] CreateDocumentRequest req)
        {
            var uploaderID = GetCurrentUserID();
            if (uploaderID == 0)
                return Unauthorized(new { message = "User identity not found." });

            var result = await _service.CreateAsync(req, uploaderID);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // PATCH /api/documents/{id}/submit  — submit draft for RO review
        [HttpPatch("{id}/submit")]
        [Authorize(Roles = "Admin,ClinicalTrialManager")]
        public async Task<IActionResult> Submit(long id)
        {
            var result = await _service.SubmitForReviewAsync(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // PATCH /api/documents/{id}/review  — RO approves or rejects
        [HttpPatch("{id}/review")]
        [Authorize(Roles = "RegulatoryOfficer")]
        public async Task<IActionResult> Review(
            long id, [FromBody] ReviewDocumentRequest req)
        {
            var reviewerID = GetCurrentUserID();
            if (reviewerID == 0)
                return Unauthorized(new { message = "User identity not found." });

            var result = await _service.ReviewAsync(id, req, reviewerID);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // DELETE /api/documents/{id}  — Admin only, Draft or Rejected only
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ── Helper ───────────────────────────────────────────────

        private long GetCurrentUserID()
        {
            var str = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(str, out var id) ? id : 0;
        }
    }
}