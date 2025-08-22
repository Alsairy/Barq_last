using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;
using BARQ.Core.Services;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BARQ.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly BarqDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly INotificationPreferenceService _preferenceService;
        private readonly IEmailService _emailService;
        private readonly ITenantProvider _tenantProvider;

        public NotificationService(
            BarqDbContext context, 
            ILogger<NotificationService> logger,
            INotificationPreferenceService preferenceService,
            IEmailService emailService,
            ITenantProvider tenantProvider)
        {
            _context = context;
            _logger = logger;
            _preferenceService = preferenceService;
            _emailService = emailService;
            _tenantProvider = tenantProvider;
        }

        public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid tenantId, NotificationListRequest request)
        {
            var query = _context.Notifications
                .Where(n => n.TenantId == tenantId &&
                           (!request.UserId.HasValue || n.UserId == request.UserId.Value) &&
                           (string.IsNullOrEmpty(request.Type) || n.Type == request.Type) &&
                           (!request.IsRead.HasValue || n.IsRead == request.IsRead.Value))
                .AsQueryable();

            var totalCount = await query.CountAsync();
            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    Priority = n.Priority,
                    Category = n.Category,
                    IsRead = n.IsRead,
                    ReadAt = n.ReadAt,
                    ActionUrl = n.ActionUrl,
                    ActionText = n.ActionText,
                    Metadata = n.Metadata,
                    CreatedAt = n.CreatedAt,
                    ExpiryDate = n.ExpiryDate
                })
                .ToListAsync();

            return new PagedResult<NotificationDto>
            {
                Items = notifications,
                Total = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                // TotalPages is calculated automatically by PagedResult
            };
        }

        public async Task<NotificationDto?> GetNotificationByIdAsync(Guid id)
        {
            var notification = await _context.Notifications
                .Where(n => n.TenantId == _tenantProvider.GetTenantId())
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notification == null)
                return null;

            return new NotificationDto
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                Priority = notification.Priority,
                Category = notification.Category,
                IsRead = notification.IsRead,
                ReadAt = notification.ReadAt,
                ActionUrl = notification.ActionUrl,
                ActionText = notification.ActionText,
                Metadata = notification.Metadata,
                CreatedAt = notification.CreatedAt,
                ExpiryDate = notification.ExpiryDate
            };
        }

        public async Task<NotificationDto> CreateNotificationAsync(Guid tenantId, CreateNotificationRequest request)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = request.UserId,
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                Priority = request.Priority ?? "Medium",
                Category = request.Category,
                ActionUrl = request.ActionUrl,
                ActionText = request.ActionText,
                Metadata = request.Metadata,
                ExpiryDate = request.ExpiryDate,
                RequiresAction = request.RequiresAction,
                ActionData = request.ActionData,
                SourceEntity = request.SourceEntity,
                SourceEntityId = request.SourceEntityId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await ProcessNotificationChannelsAsync(notification.Id);

            _logger.LogInformation("Created notification {NotificationId} for user {UserId}", 
                notification.Id, notification.UserId);

            return new NotificationDto
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                Priority = notification.Priority,
                Category = notification.Category,
                IsRead = notification.IsRead,
                ReadAt = notification.ReadAt,
                ActionUrl = notification.ActionUrl,
                ActionText = notification.ActionText,
                Metadata = notification.Metadata,
                CreatedAt = notification.CreatedAt,
                ExpiryDate = notification.ExpiryDate
            };
        }

        public async Task<bool> MarkNotificationAsReadAsync(Guid notificationId)
        {
            var notification = await _context.Notifications
                .Where(n => n.TenantId == _tenantProvider.GetTenantId())
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification == null)
                return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkNotificationsAsReadAsync(MarkNotificationReadRequest request)
        {
            var notifications = await _context.Notifications
                .Where(n => n.TenantId == _tenantProvider.GetTenantId() && request.NotificationIds.Contains(n.Id))
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                notification.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteNotificationAsync(Guid id)
        {
            var notification = await _context.Notifications
                .Where(n => n.TenantId == _tenantProvider.GetTenantId())
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notification == null)
                return false;

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetUnreadNotificationCountAsync(Guid userId)
        {
            var user = await _context.Users
                .Where(u => u.TenantId == _tenantProvider.GetTenantId())
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return 0;
            
            return await _context.Notifications
                .Where(n => n.TenantId == user.TenantId && n.UserId == userId && !n.IsRead)
                .CountAsync();
        }

        public async Task<bool> SendEmailNotificationAsync(Guid notificationId)
        {
            var notification = await _context.Notifications
                .Include(n => n.User)
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification?.User?.Email == null)
                return false;

            var success = await _emailService.SendEmailAsync(
                notification.User.Email,
                notification.Title,
                $"<h2>{notification.Title}</h2><p>{notification.Message}</p>",
                notification.Message
            );

            if (success)
            {
                notification.IsEmailSent = true;
                notification.EmailSentAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return success;
        }

        public async Task<List<NotificationDto>> GetRecentNotificationsAsync(Guid userId, int count = 10)
        {
            var user = await _context.Users
                .Where(u => u.TenantId == _tenantProvider.GetTenantId())
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return new List<NotificationDto>();
            
            return await _context.Notifications
                .Where(n => n.TenantId == user.TenantId && n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    Priority = n.Priority,
                    Category = n.Category,
                    IsRead = n.IsRead,
                    ReadAt = n.ReadAt,
                    ActionUrl = n.ActionUrl,
                    ActionText = n.ActionText,
                    Metadata = n.Metadata,
                    CreatedAt = n.CreatedAt,
                    ExpiryDate = n.ExpiryDate
                })
                .ToListAsync();
        }

        public async Task<PagedResult<NotificationCenterDto>> GetNotificationCenterAsync(Guid userId, NotificationCenterRequest request)
        {
            var user = await _context.Users
                .Where(u => u.TenantId == _tenantProvider.GetTenantId())
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return new PagedResult<NotificationCenterDto> { Items = new List<NotificationCenterDto>() };
            
            var query = _context.Notifications
                .Where(n => n.TenantId == user.TenantId && n.UserId == userId &&
                           (string.IsNullOrEmpty(request.Category) || n.Category == request.Category) &&
                           (string.IsNullOrEmpty(request.Type) || n.Type == request.Type) &&
                           (!request.IsRead.HasValue || n.IsRead == request.IsRead.Value) &&
                           (string.IsNullOrEmpty(request.Priority) || n.Priority == request.Priority) &&
                           (!request.FromDate.HasValue || n.CreatedAt >= request.FromDate.Value) &&
                           (!request.ToDate.HasValue || n.CreatedAt <= request.ToDate.Value))
                .AsQueryable();

            var totalCount = await query.CountAsync();
            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(n => new NotificationCenterDto
                {
                    Id = n.Id.ToString(),
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    Priority = n.Priority,
                    Category = n.Category ?? "General",
                    IsRead = n.IsRead,
                    ReadAt = n.ReadAt,
                    CreatedAt = n.CreatedAt,
                    ExpiresAt = n.ExpiryDate,
                    RequiresAction = n.RequiresAction,
                    ActionUrl = n.ActionUrl,
                    ActionData = n.ActionData,
                    SourceEntity = n.SourceEntity,
                    SourceEntityId = n.SourceEntityId
                })
                .ToListAsync();

            return new PagedResult<NotificationCenterDto>
            {
                Items = notifications,
                Total = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                // TotalPages is calculated automatically by PagedResult
            };
        }

        public async Task<NotificationStatsDto> GetNotificationStatsAsync(Guid userId)
        {
            var today = DateTime.UtcNow.Date;
            var user = await _context.Users
                .Where(u => u.TenantId == _tenantProvider.GetTenantId())
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return new NotificationStatsDto();
            
            var notifications = await _context.Notifications
                .Where(n => n.TenantId == user.TenantId && n.UserId == userId)
                .ToListAsync();

            return new NotificationStatsDto
            {
                Total = notifications.Count,
                UnreadCount = notifications.Count(n => !n.IsRead),
                TodayCount = notifications.Count(n => n.CreatedAt.Date == today),
                ActionRequiredCount = notifications.Count(n => n.RequiresAction && !n.IsRead),
                TypeCounts = notifications.GroupBy(n => n.Category ?? "General")
                    .ToDictionary(g => g.Key, g => g.Count()),
                PriorityCounts = notifications.GroupBy(n => n.Priority)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        public async Task<bool> MarkNotificationsAsync(Guid userId, MarkNotificationRequest request)
        {
            var notificationIds = request.NotificationIds;
            var notifications = await _context.Notifications
                .Where(n => n.TenantId == _tenantProvider.GetTenantId() && n.UserId == userId && notificationIds.Contains(n.Id))
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = request.IsRead;
                notification.ReadAt = request.IsRead ? DateTime.UtcNow : null;
                notification.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteExpiredNotificationsAsync()
        {
            var expiredNotifications = await _context.Notifications
                .Where(n => n.ExpiryDate.HasValue && n.ExpiryDate.Value < DateTime.UtcNow)
                .ToListAsync();

            _context.Notifications.RemoveRange(expiredNotifications);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted {Count} expired notifications", expiredNotifications.Count);
            return true;
        }

        public async Task<List<NotificationCenterDto>> GetActionRequiredNotificationsAsync(Guid userId)
        {
            var user = await _context.Users
                .Where(u => u.TenantId == _tenantProvider.GetTenantId())
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return new List<NotificationCenterDto>();
            
            return await _context.Notifications
                .Where(n => n.TenantId == user.TenantId && n.UserId == userId && n.RequiresAction && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationCenterDto
                {
                    Id = n.Id.ToString(),
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    Priority = n.Priority,
                    Category = n.Category ?? "General",
                    IsRead = n.IsRead,
                    ReadAt = n.ReadAt,
                    CreatedAt = n.CreatedAt,
                    ExpiresAt = n.ExpiryDate,
                    RequiresAction = n.RequiresAction,
                    ActionUrl = n.ActionUrl,
                    ActionData = n.ActionData,
                    SourceEntity = n.SourceEntity,
                    SourceEntityId = n.SourceEntityId
                })
                .ToListAsync();
        }

        public async Task<bool> SendNotificationAsync(Guid userId, string title, string message, string type, 
            string priority = "Medium", string? category = null, bool requiresAction = false, 
            string? actionUrl = null, string? actionData = null, string? sourceEntity = null, 
            string? sourceEntityId = null, DateTime? expiresAt = null)
        {
            var user = await _context.Users
                .Where(u => u.TenantId == _tenantProvider.GetTenantId())
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return false;

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = user.TenantId,
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Priority = priority,
                Category = category,
                RequiresAction = requiresAction,
                ActionUrl = actionUrl,
                ActionData = actionData,
                SourceEntity = sourceEntity,
                SourceEntityId = sourceEntityId,
                ExpiryDate = expiresAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await ProcessNotificationChannelsAsync(notification.Id);
            return true;
        }

        public async Task<bool> SendBulkNotificationAsync(List<Guid> userIds, string title, string message, 
            string type, string priority = "Medium", string? category = null)
        {
            var users = await _context.Users
                .Where(u => u.TenantId == _tenantProvider.GetTenantId() && userIds.Contains(u.Id))
                .ToListAsync();

            var notifications = users.Select(user => new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = user.TenantId,
                UserId = user.Id,
                Title = title,
                Message = message,
                Type = type,
                Priority = priority,
                Category = category,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            foreach (var notification in notifications)
            {
                await ProcessNotificationChannelsAsync(notification.Id);
            }

            return true;
        }

        public async Task<bool> ProcessNotificationChannelsAsync(Guid notificationId)
        {
            var notification = await _context.Notifications
                .Include(n => n.User)
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification?.User == null)
                return false;

            var enabledChannels = await _preferenceService.GetEnabledChannelsAsync(
                notification.UserId.ToString(), notification.Type);

            foreach (var channel in enabledChannels)
            {
                var history = new NotificationHistory
                {
                    Id = Guid.NewGuid(),
                    NotificationId = notificationId.ToString(),
                    Channel = channel,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                try
                {
                    bool success = false;
                    switch (channel)
                    {
                        case "Email":
                            success = await _emailService.SendEmailAsync(
                                notification.User.Email ?? "",
                                notification.Title,
                                $"<h2>{notification.Title}</h2><p>{notification.Message}</p>",
                                notification.Message
                            );
                            break;
                        case "SMS":
                            success = await SendEmailNotificationAsync(notification);
                            break;
                        case "InApp":
                            success = true;
                            break;
                    }

                    history.Status = success ? "Sent" : "Failed";
                    history.SentAt = success ? DateTime.UtcNow : null;
                    if (!success)
                    {
                        history.ErrorMessage = $"Failed to send {channel} notification";
                    }
                }
                catch (Exception ex)
                {
                    history.Status = "Failed";
                    history.ErrorMessage = ex.Message;
                    _logger.LogError(ex, "Failed to send {Channel} notification {NotificationId}", 
                        channel, notificationId);
                }

                _context.NotificationHistory.Add(history);
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
