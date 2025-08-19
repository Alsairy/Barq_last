using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.DTOs
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string? Category { get; set; }
        public Guid? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsEmailSent { get; set; }
        public DateTime? EmailSentAt { get; set; }
        public string? ActionUrl { get; set; }
        public string? ActionText { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateNotificationRequest
    {
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = "Info";
        
        [Required]
        [MaxLength(50)]
        public string Priority { get; set; } = "Medium";
        
        [MaxLength(100)]
        public string? Category { get; set; }
        
        public Guid? RelatedEntityId { get; set; }
        
        [MaxLength(100)]
        public string? RelatedEntityType { get; set; }
        
        [MaxLength(1000)]
        public string? ActionUrl { get; set; }
        
        [MaxLength(100)]
        public string? ActionText { get; set; }
        
        public DateTime? ExpiryDate { get; set; }
    }

    public class NotificationListRequest
    {
        public Guid? UserId { get; set; }
        public string? Type { get; set; }
        public string? Priority { get; set; }
        public string? Category { get; set; }
        public bool? IsRead { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "CreatedAt";
        public string? SortDirection { get; set; } = "desc";
    }

    public class MarkNotificationReadRequest
    {
        [Required]
        public List<Guid> NotificationIds { get; set; } = new();
    }
}
