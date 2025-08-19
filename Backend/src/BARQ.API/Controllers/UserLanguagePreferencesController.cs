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
    public class UserLanguagePreferencesController : ControllerBase
    {
        private readonly IUserLanguagePreferenceService _userLanguagePreferenceService;
        private readonly ILogger<UserLanguagePreferencesController> _logger;

        public UserLanguagePreferencesController(IUserLanguagePreferenceService userLanguagePreferenceService, ILogger<UserLanguagePreferencesController> logger)
        {
            _userLanguagePreferenceService = userLanguagePreferenceService;
            _logger = logger;
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<PagedResult<UserLanguagePreferenceDto>>> GetUserLanguagePreferences(Guid userId, [FromQuery] ListRequest request)
        {
            try
            {
                var result = await _userLanguagePreferenceService.GetUserLanguagePreferencesAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user language preferences for user: {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving user language preferences");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserLanguagePreferenceDto>> GetUserLanguagePreference(Guid id)
        {
            try
            {
                var preference = await _userLanguagePreferenceService.GetUserLanguagePreferenceByIdAsync(id);
                if (preference == null)
                {
                    return NotFound();
                }

                return Ok(preference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user language preference: {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the user language preference");
            }
        }

        [HttpGet("user/{userId}/default")]
        public async Task<ActionResult<UserLanguagePreferenceDto>> GetUserDefaultLanguagePreference(Guid userId)
        {
            try
            {
                var preference = await _userLanguagePreferenceService.GetUserDefaultLanguagePreferenceAsync(userId);
                if (preference == null)
                {
                    return NotFound("No default language preference found for user");
                }

                return Ok(preference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user default language preference: {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving the default language preference");
            }
        }

        [HttpGet("user/{userId}/language/{languageCode}")]
        public async Task<ActionResult<UserLanguagePreferenceDto>> GetUserLanguagePreferenceByCode(Guid userId, string languageCode)
        {
            try
            {
                var preference = await _userLanguagePreferenceService.GetUserLanguagePreferenceByCodeAsync(userId, languageCode);
                if (preference == null)
                {
                    return NotFound();
                }

                return Ok(preference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user language preference by code: {UserId}/{LanguageCode}", userId, languageCode);
                return StatusCode(500, "An error occurred while retrieving the user language preference");
            }
        }

        [HttpPost("user/{userId}/language/{languageId}")]
        public async Task<ActionResult<UserLanguagePreferenceDto>> CreateUserLanguagePreference(Guid userId, string languageId)
        {
            try
            {
                var currentUserId = User.Identity?.Name ?? "Unknown";
                var preference = await _userLanguagePreferenceService.CreateUserLanguagePreferenceAsync(userId, languageId, currentUserId);
                return CreatedAtAction(nameof(GetUserLanguagePreference), new { id = preference.Id }, preference);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user language preference");
                return StatusCode(500, "An error occurred while creating the user language preference");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<UserLanguagePreferenceDto>> UpdateUserLanguagePreference(Guid id, [FromBody] UpdateUserLanguagePreferenceRequest request)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var preference = await _userLanguagePreferenceService.UpdateUserLanguagePreferenceAsync(id, request, userId);
                if (preference == null)
                {
                    return NotFound();
                }

                return Ok(preference);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user language preference: {Id}", id);
                return StatusCode(500, "An error occurred while updating the user language preference");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUserLanguagePreference(Guid id)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var success = await _userLanguagePreferenceService.DeleteUserLanguagePreferenceAsync(id, userId);
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user language preference: {Id}", id);
                return StatusCode(500, "An error occurred while deleting the user language preference");
            }
        }

        [HttpPost("user/{userId}/preference/{preferenceId}/set-default")]
        public async Task<ActionResult> SetDefaultLanguagePreference(Guid userId, Guid preferenceId)
        {
            try
            {
                var currentUserId = User.Identity?.Name ?? "Unknown";
                var success = await _userLanguagePreferenceService.SetDefaultLanguagePreferenceAsync(userId, preferenceId, currentUserId);
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default language preference: {UserId}/{PreferenceId}", userId, preferenceId);
                return StatusCode(500, "An error occurred while setting the default language preference");
            }
        }

        [HttpGet("user/{userId}/accessibility")]
        public async Task<ActionResult<Dictionary<string, object>>> GetUserAccessibilitySettings(Guid userId)
        {
            try
            {
                var settings = await _userLanguagePreferenceService.GetUserAccessibilitySettingsAsync(userId);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user accessibility settings: {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving accessibility settings");
            }
        }

        [HttpPut("user/{userId}/accessibility")]
        public async Task<ActionResult> UpdateUserAccessibilitySettings(Guid userId, [FromBody] UpdateUserLanguagePreferenceRequest request)
        {
            try
            {
                var currentUserId = User.Identity?.Name ?? "Unknown";
                var success = await _userLanguagePreferenceService.UpdateUserAccessibilitySettingsAsync(userId, request, currentUserId);
                if (!success)
                {
                    return NotFound("No default language preference found for user");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user accessibility settings: {UserId}", userId);
                return StatusCode(500, "An error occurred while updating accessibility settings");
            }
        }

        [HttpGet("accessibility-users")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<List<UserLanguagePreferenceDto>>> GetUsersWithAccessibilityNeeds()
        {
            try
            {
                var users = await _userLanguagePreferenceService.GetUsersWithAccessibilityNeedsAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users with accessibility needs");
                return StatusCode(500, "An error occurred while retrieving users with accessibility needs");
            }
        }

        [HttpGet("usage-stats")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<Dictionary<string, object>>> GetLanguageUsageStats()
        {
            try
            {
                var stats = await _userLanguagePreferenceService.GetLanguageUsageStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting language usage stats");
                return StatusCode(500, "An error occurred while retrieving language usage statistics");
            }
        }
    }
}
