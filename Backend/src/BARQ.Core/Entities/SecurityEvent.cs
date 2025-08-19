using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("SecurityEvents")]
    public class SecurityEvent : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Severity { get; set; } = "Medium";
        
        [Required]
        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;
        
        public Guid? UserId { get; set; }
        
        [MaxLength(255)]
        public string? UserName { get; set; }
        
        [MaxLength(100)]
        public string? IpAddress { get; set; }
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        [MaxLength(255)]
        public string? Resource { get; set; }
        
        [MaxLength(100)]
        public string? Action { get; set; }
        
        [MaxLength(50)]
        public string? Result { get; set; }
        
        [MaxLength(2000)]
        public string? AdditionalData { get; set; }
        
        public bool IsResolved { get; set; } = false;
        public DateTime? ResolvedAt { get; set; }
        
        [MaxLength(255)]
        public string? ResolvedBy { get; set; }
        
        [MaxLength(1000)]
        public string? ResolutionNotes { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }
}
