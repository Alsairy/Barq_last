using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BARQ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FeatureFlagsController : ControllerBase
    {
        private readonly IFeatureFlagService _featureFlagService;
        private readonly ILogger<FeatureFlagsController> _logger;

        public FeatureFlagsController(IFeatureFlagService featureFlagService, ILogger<FeatureFlagsController> logger)
        {
            _featureFlagService = featureFlagService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<FeatureFlagDto>>> GetFeatureFlags([FromQuery] ListRequest request)
        {
            try
            {
                var result = await _featureFlagService.GetFeatureFlagsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feature flags");
                return StatusCode(500, "An error occurred while retrieving feature flags");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FeatureFlagDto>> GetFeatureFlag(Guid id)
        {
            try
            {
                var featureFlag = await _featureFlagService.GetFeatureFlagByIdAsync(id);
                if (featureFlag == null)
                {
                    return NotFound();
                }

                return Ok(featureFlag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feature flag: {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the feature flag");
            }
        }

        [HttpGet("by-name/{name}")]
        public async Task<ActionResult<FeatureFlagDto>> GetFeatureFlagByName(string name)
        {
            try
            {
                var featureFlag = await _featureFlagService.GetFeatureFlagByNameAsync(name);
                if (featureFlag == null)
                {
                    return NotFound();
                }

                return Ok(featureFlag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feature flag by name: {Name}", name);
                return StatusCode(500, "An error occurred while retrieving the feature flag");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<FeatureFlagDto>> CreateFeatureFlag([FromBody] CreateFeatureFlagRequest request)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var featureFlag = await _featureFlagService.CreateFeatureFlagAsync(request, userId);
                return CreatedAtAction(nameof(GetFeatureFlag), new { id = featureFlag.Id }, featureFlag);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating feature flag");
                return StatusCode(500, "An error occurred while creating the feature flag");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<FeatureFlagDto>> UpdateFeatureFlag(Guid id, [FromBody] UpdateFeatureFlagRequest request)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var featureFlag = await _featureFlagService.UpdateFeatureFlagAsync(id, request, userId);
                if (featureFlag == null)
                {
                    return NotFound();
                }

                return Ok(featureFlag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating feature flag: {Id}", id);
                return StatusCode(500, "An error occurred while updating the feature flag");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> DeleteFeatureFlag(Guid id)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var success = await _featureFlagService.DeleteFeatureFlagAsync(id, userId);
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting feature flag: {Id}", id);
                return StatusCode(500, "An error occurred while deleting the feature flag");
            }
        }

        [HttpPost("{id}/toggle")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> ToggleFeatureFlag(Guid id, [FromBody] ToggleFeatureFlagRequest request)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var success = await _featureFlagService.ToggleFeatureFlagAsync(id, request.IsEnabled, userId, request.Reason);
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling feature flag: {Id}", id);
                return StatusCode(500, "An error occurred while toggling the feature flag");
            }
        }

        [HttpGet("environment/{environment}")]
        public async Task<ActionResult<Dictionary<string, bool>>> GetFeatureFlagsForEnvironment(string environment)
        {
            try
            {
                var flags = await _featureFlagService.GetFeatureFlagsForEnvironmentAsync(environment);
                return Ok(flags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feature flags for environment: {Environment}", environment);
                return StatusCode(500, "An error occurred while retrieving feature flags");
            }
        }

        [HttpGet("category/{category}")]
        public async Task<ActionResult<List<FeatureFlagDto>>> GetFeatureFlagsByCategory(string category)
        {
            try
            {
                var flags = await _featureFlagService.GetFeatureFlagsByCategoryAsync(category);
                return Ok(flags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feature flags by category: {Category}", category);
                return StatusCode(500, "An error occurred while retrieving feature flags");
            }
        }

        [HttpPost("refresh-cache")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> RefreshFeatureFlagCache()
        {
            try
            {
                await _featureFlagService.RefreshFeatureFlagCacheAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing feature flag cache");
                return StatusCode(500, "An error occurred while refreshing the feature flag cache");
            }
        }

        [HttpGet("check/{featureName}")]
        public async Task<ActionResult<bool>> IsFeatureEnabled(string featureName, [FromQuery] string? environment = null)
        {
            try
            {
                var isEnabled = await _featureFlagService.IsFeatureEnabledAsync(featureName, environment);
                return Ok(isEnabled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if feature is enabled: {FeatureName}", featureName);
                return StatusCode(500, "An error occurred while checking the feature flag");
            }
        }
    }

    public class ToggleFeatureFlagRequest
    {
        public bool IsEnabled { get; set; }
        public string? Reason { get; set; }
    }
}
