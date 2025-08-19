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
    }
}
