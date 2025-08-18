using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("Integrations")]
    public class Integration : BaseEntity
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string IntegrationType { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Provider { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string Endpoint { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? ApiKey { get; set; }
        
        [MaxLength(2000)]
        public string? Configuration { get; set; }
        
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryCount { get; set; } = 3;
        
        [MaxLength(100)]
        public new string? Version { get; set; }
        
        public DateTime? LastSyncAt { get; set; }
        public DateTime? LastHealthCheck { get; set; }
        
        public bool IsHealthy { get; set; } = true;
        
        [MaxLength(1000)]
        public string? HealthCheckMessage { get; set; }
        
        [MaxLength(2000)]
        public string? Metadata { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
    }
}
