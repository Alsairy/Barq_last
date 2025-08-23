using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class TenantState : BaseEntity
    {
        [Required]
        public new Guid TenantId { get; set; }
        
        [MaxLength(100)]
        public string? TenantName { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Active"; // Active, Suspended, Archived, Migrating, Maintenance
        
        [MaxLength(500)]
        public string? StatusReason { get; set; }
        
        public DateTime? StatusChangedAt { get; set; }
        
        [MaxLength(100)]
        public string? StatusChangedBy { get; set; }
        
        [Required]
        public bool IsHealthy { get; set; } = true;
        
        public DateTime? LastHealthCheck { get; set; }
        
        [MaxLength(2000)]
        public string? HealthDetails { get; set; } // JSON for health metrics
        
        public long CurrentUserCount { get; set; } = 0;
        
        public long CurrentProjectCount { get; set; } = 0;
        
        public long CurrentTaskCount { get; set; } = 0;
        
        public long StorageUsedBytes { get; set; } = 0;
        
        public long APICallsThisMonth { get; set; } = 0;
        
        public long WorkflowExecutionsThisMonth { get; set; } = 0;
        
        public DateTime? LastActivityAt { get; set; }
        
        [MaxLength(100)]
        public string? LastActivityBy { get; set; }
        
        [MaxLength(50)]
        public string? DatabaseStatus { get; set; } // Healthy, Warning, Error
        
        [MaxLength(50)]
        public string? StorageStatus { get; set; } // Healthy, Warning, Error
        
        [MaxLength(50)]
        public string? BillingStatus { get; set; } // Current, Overdue, Suspended
        
        [MaxLength(2000)]
        public string? Metadata { get; set; } // JSON for additional state data
        
        public bool RequiresAttention { get; set; } = false;
        
        [MaxLength(1000)]
        public string? AttentionReason { get; set; }
        
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual ICollection<TenantStateHistory> History { get; set; } = new List<TenantStateHistory>();
    }
}
