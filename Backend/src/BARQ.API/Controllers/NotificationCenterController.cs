using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BARQ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationCenterController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationCenterController> _logger;

        public NotificationCenterController(
            INotificationService notificationService,
            ILogger<NotificationCenterController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<NotificationCenterDto>>> GetNotifications([FromQuery] NotificationCenterRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var notifications = await _notificationService.GetNotificationCenterAsync(userId, request);
            return Ok(notifications);
        }

        [HttpGet("stats")]
        public async Task<ActionResult<NotificationStatsDto>> GetStats()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var stats = await _notificationService.GetNotificationStatsAsync(userId);
            return Ok(stats);
        }

        [HttpPost("mark")]
        public async Task<ActionResult> MarkNotifications(MarkNotificationRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var success = await _notificationService.MarkNotificationsAsync(userId, request);
            if (!success)
            {
                return BadRequest("Failed to mark notifications");
            }

            return Ok();
        }

        [HttpGet("action-required")]
        public async Task<ActionResult<List<NotificationCenterDto>>> GetActionRequiredNotifications()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var notifications = await _notificationService.GetActionRequiredNotificationsAsync(userId);
            return Ok(notifications);
        }

        [HttpPost("send")]
        public async Task<ActionResult> SendNotification(SendNotificationRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var success = await _notificationService.SendNotificationAsync(
                userId,
                request.Title,
                request.Message,
                request.Type,
                request.Priority ?? "Medium",
                request.Category,
                request.RequiresAction,
                request.ActionUrl,
                request.ActionData,
                request.SourceEntity,
                request.SourceEntityId,
                request.ExpiresAt
            );

            if (!success)
            {
                return BadRequest("Failed to send notification");
            }

            return Ok();
        }

        [HttpPost("send-bulk")]
        public async Task<ActionResult> SendBulkNotification(SendBulkNotificationRequest request)
        {
            var success = await _notificationService.SendBulkNotificationAsync(
                request.UserIds,
                request.Title,
                request.Message,
                request.Type,
                request.Priority ?? "Medium",
                request.Category
            );

            if (!success)
            {
                return BadRequest("Failed to send bulk notification");
            }

            return Ok();
        }

        [HttpDelete("expired")]
        public async Task<ActionResult> DeleteExpiredNotifications()
        {
            var success = await _notificationService.DeleteExpiredNotificationsAsync();
            if (!success)
            {
                return BadRequest("Failed to delete expired notifications");
            }

            return Ok();
        }
    }

    public class SendNotificationRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Priority { get; set; }
        public string? Category { get; set; }
        public bool RequiresAction { get; set; }
        public string? ActionUrl { get; set; }
        public string? ActionData { get; set; }
        public string? SourceEntity { get; set; }
        public string? SourceEntityId { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class SendBulkNotificationRequest
    {
        public List<Guid> UserIds { get; set; } = new();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Priority { get; set; }
        public string? Category { get; set; }
    }
}
