using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class UsageQuota : BaseEntity
    {
        [Required]
        public new Guid? TenantId { get; set; }// null for plan-level quotas
        
        public Guid? BillingPlanId { get; set; } // null for tenant-specific overrides
        
        [Required]
        [MaxLength(50)]
        public string QuotaType { get; set; } = string.Empty; // Users, Projects, Tasks, Storage, APICall, WorkflowExecution
        
        [Required]
        public long QuotaLimit { get; set; }
        
        public long CurrentUsage { get; set; } = 0;
        
        [Required]
        [MaxLength(20)]
        public string ResetPeriod { get; set; } = "Monthly"; // Daily, Weekly, Monthly, Yearly, Never
        
        public DateTime? LastResetDate { get; set; }
        
        public DateTime? NextResetDate { get; set; }
        
        public bool IsHardLimit { get; set; } = true; // true = block when exceeded, false = allow with overage
        
        public decimal? OverageRate { get; set; } // cost per unit over quota
        
        public bool SendWarningAt80Percent { get; set; } = true;
        
        public bool SendWarningAt95Percent { get; set; } = true;
        
        public bool IsActive { get; set; } = true;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public virtual Tenant? Tenant { get; set; }
        public virtual BillingPlan? BillingPlan { get; set; }
        public virtual ICollection<UsageRecord> UsageRecords { get; set; } = new List<UsageRecord>();
    }
}
