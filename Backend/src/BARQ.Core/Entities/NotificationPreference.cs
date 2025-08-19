using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class NotificationPreference : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string NotificationType { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Channel { get; set; } = string.Empty; // InApp, Email, SMS
        
        public bool IsEnabled { get; set; } = true;
        
        public string? Settings { get; set; } // JSON for channel-specific settings
        
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
