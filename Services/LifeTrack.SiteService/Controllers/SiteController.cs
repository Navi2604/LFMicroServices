using LifeTrack.SiteService.DTOs;
using LifeTrack.SiteService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.SiteService.Controllers
{
    [ApiController]
    [Route("api/sites")]
    [Authorize]
    public class SiteController : ControllerBase
    {
        private readonly ISiteService _service;
        public SiteController(ISiteService service)
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

        [HttpPost]
        [Authorize(Roles = "Admin,ClinicalTrialManager")]
        public async Task<IActionResult> Create(
            [FromBody] CreateSiteRequest req)
        {
            var result = await _service.CreateAsync(req);
            return result.Success
                ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ClinicalTrialManager")]
        public async Task<IActionResult> Update(
            long id, [FromBody] CreateSiteRequest req)
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