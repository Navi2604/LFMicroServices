// ============================================================
// PatientService.API / Controllers / PatientController.cs
// DELETE ENDPOINT REMOVED — Updated
// ============================================================

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

        public PatientController(IPatientService service) => _service = service;

        // GET /api/patients?name=&email=&siteProtocolId=&enrollmentStatus=
        [HttpGet]
        [Authorize(Roles = "Admin,ClinicalTrialManager,Investigator,DataManager,RegulatoryOfficer")]
        public async Task<IActionResult> GetAll([FromQuery] PatientFilterDto filter)
            => Ok(await _service.GetAllAsync(filter));

        // GET /api/patients/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,ClinicalTrialManager,Investigator,DataManager,RegulatoryOfficer")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // POST /api/patients
        [HttpPost]
        [Authorize(Roles = "Admin,Investigator,ClinicalTrialManager")]
        public async Task<IActionResult> Create([FromBody] CreatePatientRequest req)
        {
            var result = await _service.CreateAsync(req);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ❌ DELETE REMOVED — Patients are deactivated, not deleted
    }
}