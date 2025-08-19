using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BARQ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class TenantStatesController : ControllerBase
    {
        private readonly ITenantStateService _tenantStateService;
        private readonly ILogger<TenantStatesController> _logger;

        public TenantStatesController(ITenantStateService tenantStateService, ILogger<TenantStatesController> logger)
        {
            _tenantStateService = tenantStateService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<TenantStateDto>>> GetTenantStates([FromQuery] ListRequest request)
        {
            try
            {
                var result = await _tenantStateService.GetTenantStatesAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenant states");
                return StatusCode(500, "An error occurred while retrieving tenant states");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TenantStateDto>> GetTenantState(Guid id)
        {
            try
            {
                var tenantState = await _tenantStateService.GetTenantStateByIdAsync(id);
                if (tenantState == null)
                {
                    return NotFound();
                }

                return Ok(tenantState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenant state: {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the tenant state");
            }
        }

        [HttpGet("tenant/{tenantId}")]
        public async Task<ActionResult<TenantStateDto>> GetTenantStateByTenantId(Guid tenantId)
        {
            try
            {
                var tenantState = await _tenantStateService.GetTenantStateByTenantIdAsync(tenantId);
                if (tenantState == null)
                {
                    return NotFound();
                }

                return Ok(tenantState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenant state by tenant ID: {TenantId}", tenantId);
                return StatusCode(500, "An error occurred while retrieving the tenant state");
            }
        }

        [HttpPut("tenant/{tenantId}")]
        public async Task<ActionResult<TenantStateDto>> UpdateTenantState(Guid tenantId, [FromBody] UpdateTenantStateRequest request)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var tenantState = await _tenantStateService.UpdateTenantStateAsync(tenantId, request, userId);
                if (tenantState == null)
                {
                    return NotFound();
                }

                return Ok(tenantState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tenant state: {TenantId}", tenantId);
                return StatusCode(500, "An error occurred while updating the tenant state");
            }
        }

        [HttpPost("tenant/{tenantId}/refresh")]
        public async Task<ActionResult> RefreshTenantState(Guid tenantId)
        {
            try
            {
                await _tenantStateService.RefreshTenantStateAsync(tenantId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing tenant state: {TenantId}", tenantId);
                return StatusCode(500, "An error occurred while refreshing the tenant state");
            }
        }

        [HttpPost("refresh-all")]
        public async Task<ActionResult> RefreshAllTenantStates()
        {
            try
            {
                await _tenantStateService.RefreshAllTenantStatesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing all tenant states");
                return StatusCode(500, "An error occurred while refreshing tenant states");
            }
        }

        [HttpGet("requiring-attention")]
        public async Task<ActionResult<List<TenantStateDto>>> GetTenantsRequiringAttention()
        {
            try
            {
                var tenants = await _tenantStateService.GetTenantsRequiringAttentionAsync();
                return Ok(tenants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenants requiring attention");
                return StatusCode(500, "An error occurred while retrieving tenants requiring attention");
            }
        }

        [HttpGet("unhealthy")]
        public async Task<ActionResult<List<TenantStateDto>>> GetUnhealthyTenants()
        {
            try
            {
                var tenants = await _tenantStateService.GetUnhealthyTenantsAsync();
                return Ok(tenants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unhealthy tenants");
                return StatusCode(500, "An error occurred while retrieving unhealthy tenants");
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<Dictionary<string, object>>> GetTenantStatsSummary()
        {
            try
            {
                var stats = await _tenantStateService.GetTenantStatsSummaryAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenant stats summary");
                return StatusCode(500, "An error occurred while retrieving tenant statistics");
            }
        }

        [HttpPost("tenant/{tenantId}/mark-attention")]
        public async Task<ActionResult> MarkTenantForAttention(Guid tenantId, [FromBody] MarkAttentionRequest request)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                await _tenantStateService.MarkTenantForAttentionAsync(tenantId, request.Reason, userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking tenant for attention: {TenantId}", tenantId);
                return StatusCode(500, "An error occurred while marking tenant for attention");
            }
        }

        [HttpPost("tenant/{tenantId}/clear-attention")]
        public async Task<ActionResult> ClearTenantAttention(Guid tenantId)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                await _tenantStateService.ClearTenantAttentionAsync(tenantId, userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing tenant attention: {TenantId}", tenantId);
                return StatusCode(500, "An error occurred while clearing tenant attention");
            }
        }

        [HttpPost("tenant/{tenantId}/update-usage")]
        public async Task<ActionResult> UpdateTenantUsageStats(Guid tenantId)
        {
            try
            {
                await _tenantStateService.UpdateTenantUsageStatsAsync(tenantId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tenant usage stats: {TenantId}", tenantId);
                return StatusCode(500, "An error occurred while updating tenant usage statistics");
            }
        }
    }

    public class MarkAttentionRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
