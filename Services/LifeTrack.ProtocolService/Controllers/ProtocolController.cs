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
        public ProtocolController(IProtocolService service)
            => _service = service;

        // GET /api/protocols
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.GetAllAsync());

        // GET /api/protocols/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.Success
                ? Ok(result) : NotFound(result);
        }

        // POST /api/protocols
        [HttpPost]
        [Authorize(Roles = "Admin,ClinicalTrialManager")]
        public async Task<IActionResult> Create(
            [FromBody] CreateProtocolRequest req)
        {
            var result = await _service.CreateAsync(req);
            return result.Success
                ? Ok(result) : BadRequest(result);
        }

        // PUT /api/protocols/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ClinicalTrialManager")]
        public async Task<IActionResult> Update(
            long id, [FromBody] CreateProtocolRequest req)
        {
            var result = await _service.UpdateAsync(id, req);
            return result.Success
                ? Ok(result) : BadRequest(result);
        }

        // DELETE /api/protocols/{id}
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