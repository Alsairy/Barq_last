using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class UsageRecord : BaseEntity
    {
        [Required]
        public new Guid TenantId { get; set; }
        
        public Guid? SubscriptionId { get; set; }
        
        public Guid? QuotaId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string UsageType { get; set; } = string.Empty; // Users, Projects, Tasks, Storage, APICall, WorkflowExecution
        
        [Required]
        public long Quantity { get; set; }
        
        [Required]
        public DateTime RecordedAt { get; set; }
        
        [MaxLength(100)]
        public string? EntityId { get; set; } // ID of the entity that generated usage
        
        [MaxLength(50)]
        public string? EntityType { get; set; } // Type of entity (Task, Project, etc.)
        
        [MaxLength(1000)]
        public string? Metadata { get; set; } // JSON for additional context
        
        public decimal? Cost { get; set; } // calculated cost for this usage
        
        public bool IsBillable { get; set; } = true;
        
        [MaxLength(20)]
        public string BillingPeriod { get; set; } = string.Empty; // YYYY-MM format
        
        public bool IsProcessed { get; set; } = false; // for billing processing
        
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual TenantSubscription? Subscription { get; set; }
        public virtual UsageQuota? Quota { get; set; }
    }
}
