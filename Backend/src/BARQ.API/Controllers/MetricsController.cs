using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;

namespace BARQ.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class MetricsController : ControllerBase
{
    private readonly ISystemHealthService _systemHealthService;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(ISystemHealthService systemHealthService, ILogger<MetricsController> logger)
    {
        _systemHealthService = systemHealthService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<MetricsDto>> GetMetrics()
    {
        try
        {
            var metrics = await _systemHealthService.GetMetricsAsync();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve metrics");
            return StatusCode(500, "Failed to retrieve metrics");
        }
    }

    [HttpGet("provider-performance")]
    public async Task<ActionResult<Dictionary<string, object>>> GetProviderPerformance()
    {
        try
        {
            var performance = await _systemHealthService.GetProviderPerformanceAsync();
            return Ok(performance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve provider performance metrics");
            return StatusCode(500, "Failed to retrieve provider performance metrics");
        }
    }

    [HttpGet("sla-violations")]
    public async Task<ActionResult<object>> GetSlaViolations([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        try
        {
            var violations = await _systemHealthService.GetSlaViolationMetricsAsync(from, to);
            return Ok(violations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve SLA violation metrics");
            return StatusCode(500, "Failed to retrieve SLA violation metrics");
        }
    }
}
