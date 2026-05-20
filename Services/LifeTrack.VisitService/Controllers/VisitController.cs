using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LifeTrack.Shared;
using LifeTrack.Shared.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitService.API.Services;

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
        public async Task<ActionResult<ApiResponse<List<VisitDto>>>> GetAllVisits()
        {
            try
            {
                var visits = await _visitService.GetAllAsync();
                return Ok(ApiResponse<List<VisitDto>>.Ok(visits, "Visits retrieved successfully"));
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
                if (visit == null)
                    return NotFound(ApiResponse<VisitDto>.Fail($"Visit {id} not found"));

                return Ok(ApiResponse<VisitDto>.Ok(visit, "Visit retrieved successfully"));
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
                return Ok(ApiResponse<VisitDto>.Ok(visit, "Visit created successfully"));
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
        public async Task<ActionResult<ApiResponse<bool>>> UpdateVisitStatus(long id, [FromBody] dynamic request)
        {
            try
            {
                if (request == null || request.status == null)
                    return BadRequest(ApiResponse<bool>.Fail("Status is required"));

                var result = await _visitService.UpdateStatusAsync(id, request.status);
                if (!result)
                    return NotFound(ApiResponse<bool>.Fail($"Visit {id} not found"));

                return Ok(ApiResponse<bool>.Ok(true, "Visit status updated successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<bool>.Fail(ex.Message));
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
                if (!result)
                    return BadRequest(ApiResponse<bool>.Fail("Visit not found or cannot be deleted (only Scheduled visits can be deleted)"));

                return Ok(ApiResponse<bool>.Ok(true, "Visit deleted successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.Fail($"Error deleting visit: {ex.Message}"));
            }
        }
    }
}