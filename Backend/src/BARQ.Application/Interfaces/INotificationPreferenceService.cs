using BARQ.Core.DTOs;

namespace BARQ.Application.Interfaces
{
    public interface INotificationPreferenceService
    {
        Task<NotificationPreferencesResponse> GetUserPreferencesAsync(string userId);
        Task<NotificationPreferenceDto> CreatePreferenceAsync(string userId, CreateNotificationPreferenceRequest request);
        Task<NotificationPreferenceDto> UpdatePreferenceAsync(string userId, string preferenceId, UpdateNotificationPreferenceRequest request);
        Task<bool> DeletePreferenceAsync(string userId, string preferenceId);
        Task<bool> ShouldSendNotificationAsync(string userId, string notificationType, string channel);
        Task<List<string>> GetEnabledChannelsAsync(string userId, string notificationType);
        Task SetDefaultPreferencesAsync(string userId);
        Task<Dictionary<string, object>> GetChannelSettingsAsync(string userId, string notificationType, string channel);
    }
}
