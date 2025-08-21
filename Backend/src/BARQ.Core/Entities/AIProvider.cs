using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("AIProviders")]
    public class AIProvider : BaseEntity
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string ProviderType { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Type { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string ApiEndpoint { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? ApiKey { get; set; }
        
        [MaxLength(2000)]
        public string? Configuration { get; set; }
        
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        
        public int Priority { get; set; } = 0;
        public int MaxConcurrentRequests { get; set; } = 10;
        public int TimeoutSeconds { get; set; } = 300;
        
        [MaxLength(100)]
        public new string? Version { get; set; }
        
        public DateTime? LastHealthCheck { get; set; }
        public bool IsHealthy { get; set; } = true;
        
        [MaxLength(1000)]
        public string? HealthCheckMessage { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
        
        public virtual ICollection<AIAgent> AIAgents { get; set; } = new List<AIAgent>();
        public virtual ICollection<TaskExecution> TaskExecutions { get; set; } = new List<TaskExecution>();
    }
}
