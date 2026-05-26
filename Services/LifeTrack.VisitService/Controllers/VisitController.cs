// ============================================================
// VisitService.API / Controllers / VisitController.cs
// DELETE RESTRICTED TO SCHEDULED VISITS — Updated
// ============================================================

using LifeTrack.Shared.Wrappers;
using LifeTrack.VisitService.DTOs;
using LifeTrack.VisitService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VisitService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VisitsController : ControllerBase
    {
        private readonly IVisitService _visitService;

        public VisitsController(IVisitService visitService)
        {
            _visitService = visitService;
        }

        /// <summary>
        /// Get all visits
        /// GET /api/visits
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<VisitDto>>>> GetAllVisits([FromQuery] VisitFilterDto filter)
        {
            try
            {
                var visits = await _visitService.GetAllAsync(filter);
                return Ok(visits);
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<List<VisitDto>>.Fail($"Error retrieving visits: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get visit by ID
        /// GET /api/visits/{id}
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<VisitDto>>> GetVisitById(long id)
        {
            try
            {
                var visit = await _visitService.GetByIdAsync(id);
                return visit.Success ? Ok(visit) : NotFound(visit);
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<VisitDto>.Fail($"Error retrieving visit: {ex.Message}"));
            }
        }

        /// <summary>
        /// Create a new visit
        /// POST /api/visits
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Investigator,ClinicalTrialManager")]
        public async Task<ActionResult<ApiResponse<VisitDto>>> CreateVisit([FromBody] CreateVisitRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<VisitDto>.Fail("Request body cannot be empty"));

                var visit = await _visitService.CreateAsync(request);
                return visit.Success ? Ok(visit) : BadRequest(visit);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<VisitDto>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<VisitDto>.Fail($"Error creating visit: {ex.Message}"));
            }
        }

        /// <summary>
        /// Update visit status
        /// PATCH /api/visits/{id}/status
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin,Investigator,ClinicalTrialManager")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateVisitStatus(long id, [FromBody] UpdateVisitStatusRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Status))
                    return BadRequest(ApiResponse<bool>.Fail("Status is required"));

                var result = await _visitService.UpdateStatusAsync(id, request.Status);
                return result.Success ? Ok(result) : NotFound(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.Fail($"Error updating visit: {ex.Message}"));
            }
        }

        /// <summary>
        /// Delete a visit (only Scheduled visits)
        /// DELETE /api/visits/{id}
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Investigator,ClinicalTrialManager")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteVisit(long id)
        {
            try
            {
                var result = await _visitService.DeleteAsync(id);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.Fail($"Error deleting visit: {ex.Message}"));
            }
        }
    }
}