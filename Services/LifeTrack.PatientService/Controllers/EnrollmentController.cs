// ============================================================
// PatientService.API / Controllers / EnrollmentController.cs
// ============================================================

using LifeTrack.PatientService.DTOs;
using LifeTrack.PatientService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.PatientService.Controllers
{
    [ApiController]
    [Route("api/enrollments")]
    [Authorize]
    public class EnrollmentController : ControllerBase
    {
        private readonly IEnrollmentService _service;

        public EnrollmentController(IEnrollmentService service) => _service = service;

        // GET /api/enrollments?patientId=&siteProtocolId=&status=
        [HttpGet]
        [Authorize(Roles = "Admin,ClinicalTrialManager,Investigator,DataManager,RegulatoryOfficer")]
        public async Task<IActionResult> GetAll([FromQuery] EnrollmentFilterDto filter)
            => Ok(await _service.GetAllAsync(filter));

        // GET /api/enrollments/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,ClinicalTrialManager,Investigator,DataManager,RegulatoryOfficer")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // POST /api/enrollments
        [HttpPost]
        [Authorize(Roles = "Admin,Investigator,ClinicalTrialManager")]
        public async Task<IActionResult> Enroll([FromBody] EnrollPatientRequest req)
        {
            var result = await _service.EnrollAsync(req);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // PATCH /api/enrollments/{id}/status
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin,Investigator,ClinicalTrialManager")]
        public async Task<IActionResult> UpdateStatus(
            long id, [FromBody] UpdateEnrollmentStatusRequest req)
        {
            var result = await _service.UpdateStatusAsync(id, req);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}