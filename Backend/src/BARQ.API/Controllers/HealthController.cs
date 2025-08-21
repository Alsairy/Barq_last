using Microsoft.AspNetCore.Mvc;
using BARQ.Application.Interfaces;
using BARQ.Core.Models.Responses;

namespace BARQ.API.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly ISystemHealthService _systemHealthService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(ISystemHealthService systemHealthService, ILogger<HealthController> logger)
    {
        _systemHealthService = systemHealthService;
        _logger = logger;
    }

    [HttpGet("live")]
    public async Task<IActionResult> GetLiveness()
    {
        try
        {
            var health = await _systemHealthService.GetLivenessAsync();
            return health.IsHealthy ? Ok(health) : StatusCode(503, health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Liveness check failed");
            return StatusCode(503, new { Status = "Unhealthy", Error = ex.Message });
        }
    }

    [HttpGet("ready")]
    public async Task<IActionResult> GetReadiness()
    {
        try
        {
            var health = await _systemHealthService.GetReadinessAsync();
            return health.IsHealthy ? Ok(health) : StatusCode(503, health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");
            return StatusCode(503, new { Status = "Unhealthy", Error = ex.Message });
        }
    }

    [HttpGet("flowable")]
    public async Task<IActionResult> GetFlowableHealth()
    {
        try
        {
            var health = await _systemHealthService.GetFlowableHealthAsync();
            return health.IsHealthy ? Ok(health) : StatusCode(503, health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Flowable health check failed");
            return StatusCode(503, new { Status = "Unhealthy", Error = ex.Message });
        }
    }

    [HttpGet("ai")]
    public async Task<IActionResult> GetAiHealth()
    {
        try
        {
            var health = await _systemHealthService.GetAiProvidersHealthAsync();
            return health.IsHealthy ? Ok(health) : StatusCode(503, health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI providers health check failed");
            return StatusCode(503, new { Status = "Unhealthy", Error = ex.Message });
        }
    }
}
