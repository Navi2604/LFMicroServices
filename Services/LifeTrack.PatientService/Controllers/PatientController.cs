using System.Security.Claims;
using LifeTrack.PatientService.DTOs;
using LifeTrack.PatientService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.PatientService.Controllers
{
    [ApiController]
    [Route("api/patients")]
    [Authorize]
    public class PatientController : ControllerBase
    {
        private readonly IPatientService _service;
        public PatientController(IPatientService service)
            => _service = service;

        // GET /api/patients
        [HttpGet]
        [Authorize(Roles = "Admin,ClinicalTrialManager,Investigator,DataManager,RegulatoryOfficer")]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.GetAllAsync());

        // GET /api/patients/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.Success
                ? Ok(result) : NotFound(result);
        }

        // POST /api/patients/enroll
        [HttpPost("enroll")]
        [Authorize(Roles = "Investigator")]
        public async Task<IActionResult> Enroll(
            [FromBody] EnrollPatientRequest req)
        {
            var investigatorId = long.Parse(
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);
            var result = await _service.EnrollAsync(
                req, investigatorId);
            return result.Success
                ? Ok(result) : BadRequest(result);
        }

        // PATCH /api/patients/{id}/status
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Investigator,Admin")]
        public async Task<IActionResult> UpdateStatus(
            long id, [FromBody] UpdateStatusRequest req)
        {
            var result = await _service
                .UpdateStatusAsync(id, req);
            return result.Success
                ? Ok(result) : BadRequest(result);
        }

        // DELETE /api/patients/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);
            return result.Success
                ? Ok(result) : BadRequest(result);
        }
    }
}