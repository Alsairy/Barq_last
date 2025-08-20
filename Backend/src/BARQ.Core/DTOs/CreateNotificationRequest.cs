using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.DTOs
{
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
        
        [MaxLength(2000)]
        public string? Metadata { get; set; }
        
        public DateTime? ExpiryDate { get; set; }

        public bool RequiresAction { get; set; } = false;
        public string? ActionData { get; set; }
        
        [MaxLength(100)]
        public string? SourceEntity { get; set; }
        public string? SourceEntityId { get; set; }
    }
}
