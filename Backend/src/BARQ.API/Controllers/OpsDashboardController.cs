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
    public class OpsDashboardController : ControllerBase
    {
        private readonly ISystemHealthService _systemHealthService;
        private readonly ILogger<OpsDashboardController> _logger;

        public OpsDashboardController(ISystemHealthService systemHealthService, ILogger<OpsDashboardController> logger)
        {
            _systemHealthService = systemHealthService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<OpsDashboardDto>> GetOpsDashboard()
        {
            try
            {
                var dashboard = await _systemHealthService.GetOpsDashboardAsync();
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ops dashboard");
                return StatusCode(500, "An error occurred while retrieving the operations dashboard");
            }
        }

        [HttpGet("system-health")]
        public async Task<ActionResult<PagedResult<SystemHealthDto>>> GetSystemHealth([FromQuery] ListRequest request)
        {
            try
            {
                var result = await _systemHealthService.GetSystemHealthAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health");
                return StatusCode(500, "An error occurred while retrieving system health");
            }
        }

        [HttpGet("system-health/{id}")]
        public async Task<ActionResult<SystemHealthDto>> GetSystemHealthById(Guid id)
        {
            try
            {
                var health = await _systemHealthService.GetSystemHealthByIdAsync(id);
                if (health == null)
                {
                    return NotFound();
                }

                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health by ID: {Id}", id);
                return StatusCode(500, "An error occurred while retrieving system health");
            }
        }

        [HttpGet("system-health/component/{component}")]
        public async Task<ActionResult<SystemHealthDto>> GetSystemHealthByComponent(string component)
        {
            try
            {
                var health = await _systemHealthService.GetSystemHealthByComponentAsync(component);
                if (health == null)
                {
                    return NotFound();
                }

                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health by component: {Component}", component);
                return StatusCode(500, "An error occurred while retrieving system health");
            }
        }

        [HttpGet("system-health/status/{status}")]
        public async Task<ActionResult<List<SystemHealthDto>>> GetSystemHealthByStatus(string status)
        {
            try
            {
                var healthRecords = await _systemHealthService.GetSystemHealthByStatusAsync(status);
                return Ok(healthRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health by status: {Status}", status);
                return StatusCode(500, "An error occurred while retrieving system health");
            }
        }

        [HttpPost("system-health/refresh")]
        public async Task<ActionResult> RefreshAllHealthChecks()
        {
            try
            {
                await _systemHealthService.RefreshAllHealthChecksAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing all health checks");
                return StatusCode(500, "An error occurred while refreshing health checks");
            }
        }

        [HttpGet("system-health/is-healthy")]
        public async Task<ActionResult<bool>> IsSystemHealthy()
        {
            try
            {
                var isHealthy = await _systemHealthService.IsSystemHealthyAsync();
                return Ok(isHealthy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if system is healthy");
                return StatusCode(500, "An error occurred while checking system health");
            }
        }

        [HttpGet("system-metrics")]
        public async Task<ActionResult<Dictionary<string, object>>> GetSystemMetrics()
        {
            try
            {
                var metrics = await _systemHealthService.GetSystemMetricsAsync();
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system metrics");
                return StatusCode(500, "An error occurred while retrieving system metrics");
            }
        }

        [HttpPost("system-health/cleanup")]
        public async Task<ActionResult> CleanupOldHealthRecords([FromQuery] int daysToKeep = 30)
        {
            try
            {
                await _systemHealthService.CleanupOldHealthRecordsAsync(daysToKeep);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old health records");
                return StatusCode(500, "An error occurred while cleaning up old health records");
            }
        }
    }
}
