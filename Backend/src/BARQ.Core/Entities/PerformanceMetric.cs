using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("PerformanceMetrics")]
    public class PerformanceMetric : IEntity, IAuditable
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid? TenantId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string MetricType { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string MetricName { get; set; } = string.Empty;
        
        public decimal Value { get; set; } = 0;
        
        [MaxLength(50)]
        public string? Unit { get; set; }
        
        [MaxLength(100)]
        public string? Category { get; set; }
        
        [MaxLength(2000)]
        public string? Metadata { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public Guid? DeletedBy { get; set; }
        public int Version { get; set; } = 1;

        [ForeignKey("TenantId")]
        public virtual Tenant? Tenant { get; set; }
    }
}
