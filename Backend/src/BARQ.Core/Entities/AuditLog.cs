using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("AuditLogs")]
    public class AuditLog : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;
        
        [Required]
        public Guid EntityId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? TableName { get; set; }
        
        [MaxLength(100)]
        public string? FieldName { get; set; }
        
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        
        public Guid? UserId { get; set; }
        
        [MaxLength(255)]
        public string? UserName { get; set; }
        
        [MaxLength(100)]
        public string? IpAddress { get; set; }
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        [MaxLength(2000)]
        public string? AdditionalData { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }
}
