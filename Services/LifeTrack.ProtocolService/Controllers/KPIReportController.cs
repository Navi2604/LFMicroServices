// ============================================================
// ProtocolService.API / Controllers / KPIReportController.cs
// ============================================================

using LifeTrack.ProtocolService.DTOs;
using LifeTrack.ProtocolService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeTrack.ProtocolService.Controllers
{
    [ApiController]
    [Route("api/kpi-reports")]
    [Authorize]
    public class KPIReportController : ControllerBase
    {
        private readonly IKPIReportService _service;

        public KPIReportController(IKPIReportService service) => _service = service;

        // GET /api/kpi-reports?protocolId=&fromDate=&toDate=
        [HttpGet]
        [Authorize(Roles = "Admin,ClinicalTrialManager,RegulatoryOfficer,DataManager")]
        public async Task<IActionResult> GetAll([FromQuery] KPIReportFilterDto filter)
            => Ok(await _service.GetAllAsync(filter));

        // GET /api/kpi-reports/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,ClinicalTrialManager,RegulatoryOfficer,DataManager")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // POST /api/kpi-reports
        [HttpPost]
        [Authorize(Roles = "Admin,ClinicalTrialManager,DataManager")]
        public async Task<IActionResult> Create([FromBody] CreateKPIReportRequest req)
        {
            var result = await _service.CreateAsync(req);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // DELETE /api/kpi-reports/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}