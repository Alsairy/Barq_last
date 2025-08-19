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

}
