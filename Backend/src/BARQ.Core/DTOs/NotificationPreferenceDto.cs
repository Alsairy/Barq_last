namespace BARQ.Core.DTOs
{
    public class NotificationPreferenceDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public string? Settings { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateNotificationPreferenceRequest
    {
        public string NotificationType { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public string? Settings { get; set; }
    }

    public class UpdateNotificationPreferenceRequest
    {
        public bool IsEnabled { get; set; }
        public string? Settings { get; set; }
    }

    public class NotificationPreferencesResponse
    {
        public List<NotificationPreferenceDto> Preferences { get; set; } = new();
        public List<string> AvailableTypes { get; set; } = new();
        public List<string> AvailableChannels { get; set; } = new();
    }
}
