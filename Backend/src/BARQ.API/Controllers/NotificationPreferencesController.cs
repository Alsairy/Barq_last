using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BARQ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationPreferencesController : ControllerBase
    {
        private readonly INotificationPreferenceService _notificationPreferenceService;
        private readonly ILogger<NotificationPreferencesController> _logger;

        public NotificationPreferencesController(
            INotificationPreferenceService notificationPreferenceService,
            ILogger<NotificationPreferencesController> logger)
        {
            _notificationPreferenceService = notificationPreferenceService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<NotificationPreferencesResponse>> GetPreferences()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var preferences = await _notificationPreferenceService.GetUserPreferencesAsync(userId);
            return Ok(preferences);
        }

        [HttpPost]
        public async Task<ActionResult<NotificationPreferenceDto>> CreatePreference(CreateNotificationPreferenceRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var preference = await _notificationPreferenceService.CreatePreferenceAsync(userId, request);
                return CreatedAtAction(nameof(GetPreferences), new { id = preference.Id }, preference);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{preferenceId}")]
        public async Task<ActionResult<NotificationPreferenceDto>> UpdatePreference(
            string preferenceId, 
            UpdateNotificationPreferenceRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var preference = await _notificationPreferenceService.UpdatePreferenceAsync(userId, preferenceId, request);
                return Ok(preference);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("{preferenceId}")]
        public async Task<ActionResult> DeletePreference(string preferenceId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _notificationPreferenceService.DeletePreferenceAsync(userId, preferenceId);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPost("defaults")]
        public async Task<ActionResult> SetDefaultPreferences()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            await _notificationPreferenceService.SetDefaultPreferencesAsync(userId);
            return Ok();
        }

        [HttpGet("channels/{notificationType}/{channel}")]
        public async Task<ActionResult<Dictionary<string, object>>> GetChannelSettings(
            string notificationType, 
            string channel)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var settings = await _notificationPreferenceService.GetChannelSettingsAsync(userId, notificationType, channel);
            return Ok(settings);
        }
    }
}
