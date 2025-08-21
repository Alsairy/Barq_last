using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;
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

        public NotificationService(
            BarqDbContext context, 
            ILogger<NotificationService> logger,
            INotificationPreferenceService preferenceService,
            IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _preferenceService = preferenceService;
            _emailService = emailService;
        }

        public async System.Threading.Tasks.Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid tenantId, NotificationListRequest request)
        {
            var query = _context.Notifications
                .Where(n => n.TenantId == tenantId)
                .AsQueryable();

            if (request.UserId.HasValue)
            {
                query = query.Where(n => n.UserId == request.UserId.Value);
            }

            if (!string.IsNullOrEmpty(request.Type))
            {
                query = query.Where(n => n.Type == request.Type);
            }

            if (request.IsRead.HasValue)
            {
                query = query.Where(n => n.IsRead == request.IsRead.Value);
            }

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
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                // TotalPages is calculated automatically by PagedResult
            };
        }

        public async System.Threading.Tasks.Task<NotificationDto?> GetNotificationByIdAsync(Guid id)
        {
            var notification = await _context.Notifications
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

        public async System.Threading.Tasks.Task<NotificationDto> CreateNotificationAsync(Guid tenantId, CreateNotificationRequest request)
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

        public async System.Threading.Tasks.Task<bool> MarkNotificationAsReadAsync(Guid notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification == null)
                return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async System.Threading.Tasks.Task<bool> MarkNotificationsAsReadAsync(MarkNotificationReadRequest request)
        {
            var notifications = await _context.Notifications
                .Where(n => request.NotificationIds.Contains(n.Id))
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

        public async System.Threading.Tasks.Task<bool> DeleteNotificationAsync(Guid id)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notification == null)
                return false;

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }

        public async System.Threading.Tasks.Task<int> GetUnreadNotificationCountAsync(Guid userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }

        public async System.Threading.Tasks.Task<bool> SendEmailNotificationAsync(Guid notificationId)
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

        public async System.Threading.Tasks.Task<List<NotificationDto>> GetRecentNotificationsAsync(Guid userId, int count = 10)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
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

        public async System.Threading.Tasks.Task<PagedResult<NotificationCenterDto>> GetNotificationCenterAsync(Guid userId, NotificationCenterRequest request)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Category))
            {
                query = query.Where(n => n.Category == request.Category);
            }

            if (!string.IsNullOrEmpty(request.Type))
            {
                query = query.Where(n => n.Type == request.Type);
            }

            if (request.IsRead.HasValue)
            {
                query = query.Where(n => n.IsRead == request.IsRead.Value);
            }

            if (!string.IsNullOrEmpty(request.Priority))
            {
                query = query.Where(n => n.Priority == request.Priority);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(n => n.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(n => n.CreatedAt <= request.ToDate.Value);
            }

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
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                // TotalPages is calculated automatically by PagedResult
            };
        }

        public async System.Threading.Tasks.Task<NotificationStatsDto> GetNotificationStatsAsync(Guid userId)
        {
            var today = DateTime.UtcNow.Date;
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .ToListAsync();

            return new NotificationStatsDto
            {
                TotalCount = notifications.Count,
                UnreadCount = notifications.Count(n => !n.IsRead),
                TodayCount = notifications.Count(n => n.CreatedAt.Date == today),
                ActionRequiredCount = notifications.Count(n => n.RequiresAction && !n.IsRead),
                TypeCounts = notifications.GroupBy(n => n.Category ?? "General")
                    .ToDictionary(g => g.Key, g => g.Count()),
                PriorityCounts = notifications.GroupBy(n => n.Priority)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        public async System.Threading.Tasks.Task<bool> MarkNotificationsAsync(Guid userId, MarkNotificationRequest request)
        {
            var notificationIds = request.NotificationIds;
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && notificationIds.Contains(n.Id))
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

        public async System.Threading.Tasks.Task<bool> DeleteExpiredNotificationsAsync()
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
            return await _context.Notifications
                .Where(n => n.UserId == userId && n.RequiresAction && !n.IsRead)
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
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
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
                .Where(u => userIds.Contains(u.Id))
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
                            success = true; // Placeholder
                            break;
                        case "InApp":
                            success = true; // In-app notifications are already created
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
