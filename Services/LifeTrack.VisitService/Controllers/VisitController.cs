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
        public VisitController(IVisitService service)
            => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.Success
                ? Ok(result) : NotFound(result);
        }

        [HttpGet("patient/{patientId}")]
        public async Task<IActionResult> GetByPatient(
            long patientId)
            => Ok(await _service.GetByPatientAsync(patientId));

        [HttpPost]
        [Authorize(Roles = "Admin,Investigator,ClinicalTrialManager")]
        public async Task<IActionResult> Create(
            [FromBody] CreateVisitRequest req)
        {
            var result = await _service.CreateAsync(req);
            return result.Success
                ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Investigator,ClinicalTrialManager")]
        public async Task<IActionResult> Update(
            long id, [FromBody] CreateVisitRequest req)
        {
            var result = await _service.UpdateAsync(id, req);
            return result.Success
                ? Ok(result) : BadRequest(result);
        }

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