using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class NotificationHistory : BaseEntity
    {
        [Required]
        public string NotificationId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Channel { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty; // Sent, Failed, Delivered, Read
        
        public DateTime? SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
        
        public string? ErrorMessage { get; set; }
        public string? ExternalId { get; set; } // For tracking with email providers
        
        public virtual Notification Notification { get; set; } = null!;
    }
}
