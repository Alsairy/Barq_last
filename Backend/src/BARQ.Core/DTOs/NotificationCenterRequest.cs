using BARQ.Core.DTOs.Common;

namespace BARQ.Core.DTOs
{
    public class NotificationCenterRequest : ListRequest
    {
        public bool? IsRead { get; set; }
        public string? Type { get; set; }
        public string? Priority { get; set; }
        public string? Category { get; set; }
        public bool? RequiresAction { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class NotificationStatsDto
    {
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public int ActionRequiredCount { get; set; }
        public int TodayCount { get; set; }
        public int WeekCount { get; set; }
        public Dictionary<string, int> TypeCounts { get; set; } = new();
        public Dictionary<string, int> PriorityCounts { get; set; } = new();
    }

    public class MarkNotificationRequest
    {
        public List<Guid> NotificationIds { get; set; } = new();
        public bool IsRead { get; set; } = true;
    }

    public class NotificationListRequest : ListRequest
    {
        public Guid? UserId { get; set; }
        public bool? IsRead { get; set; }
        public string? Type { get; set; }
        public string? Priority { get; set; }
    }

    public class MarkNotificationReadRequest
    {
        public List<Guid> NotificationIds { get; set; } = new();
        public bool IsRead { get; set; } = true;
    }
}
