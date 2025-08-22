using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using System.Linq;

namespace BARQ.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class OpsConsoleController : ControllerBase
{
    private readonly IFeatureFlagService _featureFlagService;
    private readonly ITenantStateService _tenantStateService;
    private readonly IImpersonationService _impersonationService;
    private readonly ISystemHealthService _systemHealthService;
    private readonly ILogger<OpsConsoleController> _logger;

    public OpsConsoleController(
        IFeatureFlagService featureFlagService,
        ITenantStateService tenantStateService,
        IImpersonationService impersonationService,
        ISystemHealthService systemHealthService,
        ILogger<OpsConsoleController> logger)
    {
        _featureFlagService = featureFlagService;
        _tenantStateService = tenantStateService;
        _impersonationService = impersonationService;
        _systemHealthService = systemHealthService;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<object>> GetOpsDashboard()
    {
        try
        {
            var health = await _systemHealthService.GetLivenessAsync();
            var metrics = await _systemHealthService.GetMetricsAsync();
            
            var dashboard = new
            {
                SystemHealth = health,
                Metrics = metrics,
                Timestamp = DateTime.UtcNow
            };

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve ops dashboard");
            return StatusCode(500, "Failed to retrieve ops dashboard");
        }
    }

    [HttpGet("feature-flags")]
    public async Task<ActionResult<object>> GetFeatureFlags()
    {
        try
        {
            var flags = await _featureFlagService.GetFeatureFlagsAsync(new ListRequest { Page = 1, PageSize = 100 });
            return Ok(flags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve feature flags");
            return StatusCode(500, "Failed to retrieve feature flags");
        }
    }

    [HttpPost("feature-flags/{flagName}/toggle")]
    public async Task<ActionResult> ToggleFeatureFlag(string flagName, [FromBody] bool enabled)
    {
        try
        {
            var flag = await _featureFlagService.GetFeatureFlagByNameAsync(flagName);
            if (flag != null)
            {
                await _featureFlagService.ToggleFeatureFlagAsync(flag.Id, enabled, "OpsConsole", "Manual toggle from ops console");
            }
            _logger.LogInformation("Feature flag {FlagName} set to {Enabled} by {User}", 
                flagName, enabled, User.Identity?.Name);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle feature flag {FlagName}", flagName);
            return StatusCode(500, "Failed to toggle feature flag");
        }
    }

    [HttpGet("tenant-states")]
    public async Task<ActionResult<object>> GetTenantStates()
    {
        try
        {
            var states = await _tenantStateService.GetTenantStatesAsync(new ListRequest { Page = 1, PageSize = 100 });
            return Ok(states);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve tenant states");
            return StatusCode(500, "Failed to retrieve tenant states");
        }
    }

    [HttpPost("impersonate/{userId}")]
    public async Task<ActionResult> StartImpersonation(Guid userId, [FromBody] string reason)
    {
        try
        {
            var currentUserId = Guid.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value ?? "");
            var request = new CreateImpersonationSessionRequest 
            { 
                TargetUserId = userId, 
                Reason = reason, 
                DurationMinutes = 60 
            };
            await _impersonationService.StartImpersonationAsync(request, currentUserId.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown", HttpContext.Request.Headers["User-Agent"].ToString());
            
            _logger.LogWarning("Impersonation started: {AdminUser} impersonating {TargetUser} for reason: {Reason}",
                User.Identity?.Name, userId, reason);
            
            return Ok(new { Message = "Impersonation started successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start impersonation for user {UserId}", userId);
            return StatusCode(500, "Failed to start impersonation");
        }
    }

    [HttpPost("impersonate/stop")]
    public async Task<ActionResult> StopImpersonation()
    {
        try
        {
            var currentUserId = Guid.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value ?? "");
            var activeSessions = await _impersonationService.GetActiveImpersonationSessionsAsync();
            if (activeSessions.Any())
            {
                var sessionId = activeSessions.First().Id;
                await _impersonationService.EndImpersonationAsync(sessionId, new EndImpersonationSessionRequest { Reason = "Manual stop from ops console" }, currentUserId.ToString());
            }
            
            _logger.LogInformation("Impersonation stopped by {AdminUser}", User.Identity?.Name);
            
            return Ok(new { Message = "Impersonation stopped successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop impersonation");
            return StatusCode(500, "Failed to stop impersonation");
        }
    }
}
