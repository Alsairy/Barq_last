using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;

namespace BARQ.Application.Interfaces
{
    public interface INotificationService
    {
        Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid tenantId, NotificationListRequest request);
        Task<NotificationDto?> GetNotificationByIdAsync(Guid id);
        Task<NotificationDto> CreateNotificationAsync(Guid tenantId, CreateNotificationRequest request);
        Task<bool> MarkNotificationAsReadAsync(Guid notificationId);
        Task<bool> MarkNotificationsAsReadAsync(MarkNotificationReadRequest request);
        Task<bool> DeleteNotificationAsync(Guid id);
        Task<int> GetUnreadNotificationCountAsync(Guid userId);
        Task<bool> SendEmailNotificationAsync(Guid notificationId);
        Task<List<NotificationDto>> GetRecentNotificationsAsync(Guid userId, int count = 10);

        Task<PagedResult<NotificationCenterDto>> GetNotificationCenterAsync(Guid userId, NotificationCenterRequest request);
        Task<NotificationStatsDto> GetNotificationStatsAsync(Guid userId);
        Task<bool> MarkNotificationsAsync(Guid userId, MarkNotificationRequest request);
        Task<bool> DeleteExpiredNotificationsAsync();
        Task<List<NotificationCenterDto>> GetActionRequiredNotificationsAsync(Guid userId);

        Task<bool> SendNotificationAsync(Guid userId, string title, string message, string type, 
            string priority = "Medium", string? category = null, bool requiresAction = false, 
            string? actionUrl = null, string? actionData = null, string? sourceEntity = null, 
            string? sourceEntityId = null, DateTime? expiresAt = null);
        
        Task<bool> SendBulkNotificationAsync(List<Guid> userIds, string title, string message, 
            string type, string priority = "Medium", string? category = null);
        
        Task<bool> ProcessNotificationChannelsAsync(Guid notificationId);
    }
}
