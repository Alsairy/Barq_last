namespace BARQ.Core.DTOs
{
    public class NotificationCenterDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool RequiresAction { get; set; }
        public string? ActionUrl { get; set; }
        public string? ActionData { get; set; }
        public string? SourceEntity { get; set; }
        public string? SourceEntityId { get; set; }
    }

    public class NotificationCenterRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Category { get; set; }
        public string? Type { get; set; }
        public bool? IsRead { get; set; }
        public string? Priority { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class MarkNotificationRequest
    {
        public List<string> NotificationIds { get; set; } = new();
        public bool IsRead { get; set; } = true;
    }

    public class NotificationStatsDto
    {
        public int TotalUnread { get; set; }
        public int TotalToday { get; set; }
        public int RequiringAction { get; set; }
        public Dictionary<string, int> ByCategory { get; set; } = new();
        public Dictionary<string, int> ByPriority { get; set; } = new();
    }
}
