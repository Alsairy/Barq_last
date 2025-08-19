using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.Entities;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;

namespace BARQ.Application.Services
{
    public class NotificationPreferenceService : INotificationPreferenceService
    {
        private readonly BarqDbContext _context;
        private readonly ILogger<NotificationPreferenceService> _logger;

        private readonly List<string> _defaultNotificationTypes = new()
        {
            "TaskAssigned", "TaskCompleted", "TaskOverdue", "ProjectCreated", "ProjectUpdated",
            "WorkflowStarted", "WorkflowCompleted", "WorkflowFailed", "SystemAlert", "SecurityAlert",
            "MaintenanceNotice", "FeatureAnnouncement", "BillingAlert", "QuotaWarning"
        };

        private readonly List<string> _availableChannels = new()
        {
            "InApp", "Email", "SMS"
        };

        public NotificationPreferenceService(BarqDbContext context, ILogger<NotificationPreferenceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<NotificationPreferencesResponse> GetUserPreferencesAsync(string userId)
        {
            var userGuid = Guid.Parse(userId);
            var preferences = await _context.NotificationPreferences
                .Where(p => p.UserId == userGuid)
                .Select(p => new NotificationPreferenceDto
                {
                    Id = p.Id.ToString(),
                    UserId = p.UserId.ToString(),
                    NotificationType = p.NotificationType,
                    Channel = p.Channel,
                    IsEnabled = p.IsEnabled,
                    Settings = p.Settings,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt ?? DateTime.UtcNow
                })
                .ToListAsync();

            return new NotificationPreferencesResponse
            {
                Preferences = preferences,
                AvailableTypes = _defaultNotificationTypes,
                AvailableChannels = _availableChannels
            };
        }

        public async Task<NotificationPreferenceDto> CreatePreferenceAsync(string userId, CreateNotificationPreferenceRequest request)
        {
            var userGuid = Guid.Parse(userId);
            var existing = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userGuid && 
                                        p.NotificationType == request.NotificationType && 
                                        p.Channel == request.Channel);

            if (existing != null)
            {
                throw new InvalidOperationException("Notification preference already exists for this type and channel");
            }

            var preference = new NotificationPreference
            {
                Id = Guid.NewGuid(),
                UserId = userGuid,
                NotificationType = request.NotificationType,
                Channel = request.Channel,
                IsEnabled = request.IsEnabled,
                Settings = request.Settings,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.NotificationPreferences.Add(preference);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created notification preference for user {UserId}: {Type} via {Channel}", 
                userId, request.NotificationType, request.Channel);

            return new NotificationPreferenceDto
            {
                Id = preference.Id.ToString(),
                UserId = preference.UserId.ToString(),
                NotificationType = preference.NotificationType,
                Channel = preference.Channel,
                IsEnabled = preference.IsEnabled,
                Settings = preference.Settings,
                CreatedAt = preference.CreatedAt,
                UpdatedAt = preference.UpdatedAt ?? DateTime.UtcNow
            };
        }

        public async Task<NotificationPreferenceDto> UpdatePreferenceAsync(string userId, string preferenceId, UpdateNotificationPreferenceRequest request)
        {
            var userGuid = Guid.Parse(userId);
            var preferenceGuid = Guid.Parse(preferenceId);
            var preference = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.Id == preferenceGuid && p.UserId == userGuid);

            if (preference == null)
            {
                throw new ArgumentException("Notification preference not found");
            }

            preference.IsEnabled = request.IsEnabled;
            preference.Settings = request.Settings;
            preference.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated notification preference {PreferenceId} for user {UserId}", 
                preferenceId, userId);

            return new NotificationPreferenceDto
            {
                Id = preference.Id.ToString(),
                UserId = preference.UserId.ToString(),
                NotificationType = preference.NotificationType,
                Channel = preference.Channel,
                IsEnabled = preference.IsEnabled,
                Settings = preference.Settings,
                CreatedAt = preference.CreatedAt,
                UpdatedAt = preference.UpdatedAt ?? DateTime.UtcNow
            };
        }

        public async Task<bool> DeletePreferenceAsync(string userId, string preferenceId)
        {
            var userGuid = Guid.Parse(userId);
            var preferenceGuid = Guid.Parse(preferenceId);
            var preference = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.Id == preferenceGuid && p.UserId == userGuid);

            if (preference == null)
            {
                return false;
            }

            _context.NotificationPreferences.Remove(preference);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted notification preference {PreferenceId} for user {UserId}", 
                preferenceId, userId);

            return true;
        }

        public async Task<bool> ShouldSendNotificationAsync(string userId, string notificationType, string channel)
        {
            var userGuid = Guid.Parse(userId);
            var preference = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userGuid && 
                                        p.NotificationType == notificationType && 
                                        p.Channel == channel);

            if (preference == null)
            {
                return channel switch
                {
                    "InApp" => true,
                    "Email" => IsImportantNotificationType(notificationType),
                    "SMS" => false,
                    _ => false
                };
            }

            return preference.IsEnabled;
        }

        public async Task<List<string>> GetEnabledChannelsAsync(string userId, string notificationType)
        {
            var enabledChannels = new List<string>();

            foreach (var channel in _availableChannels)
            {
                if (await ShouldSendNotificationAsync(userId, notificationType, channel))
                {
                    enabledChannels.Add(channel);
                }
            }

            return enabledChannels;
        }

        public async System.Threading.Tasks.Task SetDefaultPreferencesAsync(string userId)
        {
            var userGuid = Guid.Parse(userId);
            var existingPreferences = await _context.NotificationPreferences
                .Where(p => p.UserId == userGuid)
                .ToListAsync();

            if (existingPreferences.Any())
            {
                return;
            }

            var defaultPreferences = new List<NotificationPreference>();

            foreach (var notificationType in _defaultNotificationTypes)
            {
                defaultPreferences.Add(new NotificationPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userGuid,
                    NotificationType = notificationType,
                    Channel = "InApp",
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                defaultPreferences.Add(new NotificationPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userGuid,
                    NotificationType = notificationType,
                    Channel = "Email",
                    IsEnabled = IsImportantNotificationType(notificationType),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                defaultPreferences.Add(new NotificationPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userGuid,
                    NotificationType = notificationType,
                    Channel = "SMS",
                    IsEnabled = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            _context.NotificationPreferences.AddRange(defaultPreferences);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created default notification preferences for user {UserId}", userId);
        }

        public async Task<Dictionary<string, object>> GetChannelSettingsAsync(string userId, string notificationType, string channel)
        {
            var userGuid = Guid.Parse(userId);
            var preference = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userGuid && 
                                        p.NotificationType == notificationType && 
                                        p.Channel == channel);

            if (preference?.Settings == null)
            {
                return new Dictionary<string, object>();
            }

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(preference.Settings) 
                       ?? new Dictionary<string, object>();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize channel settings for preference {PreferenceId}", preference.Id);
                return new Dictionary<string, object>();
            }
        }

        private static bool IsImportantNotificationType(string notificationType)
        {
            var importantTypes = new HashSet<string>
            {
                "TaskOverdue", "SystemAlert", "SecurityAlert", "MaintenanceNotice", 
                "BillingAlert", "QuotaWarning", "WorkflowFailed"
            };

            return importantTypes.Contains(notificationType);
        }
    }
}
