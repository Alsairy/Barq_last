using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Models.Responses;

namespace BARQ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private Guid GetCurrentTenantId()
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;
            return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : Guid.Empty;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<NotificationDto>>>> GetNotifications([FromQuery] NotificationListRequest request)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (request.UserId == null)
                    request.UserId = GetCurrentUserId();
                    
                var result = await _notificationService.GetNotificationsAsync(tenantId, request);
                return Ok(ApiResponse<PagedResult<NotificationDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<PagedResult<NotificationDto>>.Fail(ex.Message));
            }
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                var count = await _notificationService.GetUnreadNotificationCountAsync(userId);
                return Ok(ApiResponse<int>.Ok(count));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<int>.Fail(ex.Message));
            }
        }

        [HttpGet("recent")]
        public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetRecentNotifications([FromQuery] int count = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                var notifications = await _notificationService.GetRecentNotificationsAsync(userId, count);
                return Ok(ApiResponse<List<NotificationDto>>.Ok(notifications));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<List<NotificationDto>>.Fail(ex.Message));
            }
        }

        [HttpPost]
        [Authorize(Policy = "RequireManagerRole")]
        public async Task<ActionResult<ApiResponse<NotificationDto>>> CreateNotification([FromBody] CreateNotificationRequest request)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var notification = await _notificationService.CreateNotificationAsync(tenantId, request);
                return Ok(ApiResponse<NotificationDto>.Ok(notification, "Notification created successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<NotificationDto>.Fail(ex.Message));
            }
        }

        [HttpPost("{id}/mark-read")]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(Guid id)
        {
            try
            {
                var result = await _notificationService.MarkNotificationAsReadAsync(id);
                if (!result)
                    return NotFound(ApiResponse<bool>.Fail("Notification not found"));

                return Ok(ApiResponse<bool>.Ok(true, "Notification marked as read"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.Fail(ex.Message));
            }
        }

        [HttpPost("mark-read")]
        public async Task<ActionResult<ApiResponse<bool>>> MarkMultipleAsRead([FromBody] MarkNotificationReadRequest request)
        {
            try
            {
                var result = await _notificationService.MarkNotificationsAsReadAsync(request);
                return Ok(ApiResponse<bool>.Ok(result, "Notifications marked as read"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.Fail(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteNotification(Guid id)
        {
            try
            {
                var result = await _notificationService.DeleteNotificationAsync(id);
                if (!result)
                    return NotFound(ApiResponse<bool>.Fail("Notification not found"));

                return Ok(ApiResponse<bool>.Ok(true, "Notification deleted successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.Fail(ex.Message));
            }
        }
    }
}
