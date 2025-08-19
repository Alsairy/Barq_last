using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("Notifications")]
    public class Notification : BaseEntity
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
        
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        
        public bool IsEmailSent { get; set; } = false;
        public DateTime? EmailSentAt { get; set; }
        
        [MaxLength(1000)]
        public string? ActionUrl { get; set; }
        
        [MaxLength(100)]
        public string? ActionText { get; set; }
        
        [MaxLength(2000)]
        public string? Metadata { get; set; }
        
        public DateTime? ExpiryDate { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
