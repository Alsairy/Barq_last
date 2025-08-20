using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class BillingPlan : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public decimal Price { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string BillingCycle { get; set; } = "Monthly"; // Monthly, Yearly, OneTime
        
        [Required]
        [MaxLength(20)]
        public string PlanType { get; set; } = "Standard"; // Free, Basic, Standard, Premium, Enterprise
        
        public bool IsActive { get; set; } = true;
        
        public bool IsPublic { get; set; } = true;
        
        public int MaxUsers { get; set; } = 0; // 0 = unlimited
        
        public int MaxProjects { get; set; } = 0; // 0 = unlimited
        
        public int MaxTasks { get; set; } = 0; // 0 = unlimited
        
        public long MaxStorageBytes { get; set; } = 0; // 0 = unlimited
        
        public int MaxAPICallsPerMonth { get; set; } = 0; // 0 = unlimited
        
        public int MaxWorkflowExecutions { get; set; } = 0; // 0 = unlimited
        
        [MaxLength(2000)]
        public string? Features { get; set; } // JSON array of feature flags
        
        public int TrialDays { get; set; } = 0;
        
        public bool RequiresCreditCard { get; set; } = false;
        
        public int SortOrder { get; set; } = 0;
        
        [MaxLength(50)]
        public string? StripeProductId { get; set; }
        
        [MaxLength(50)]
        public string? StripePriceId { get; set; }
        
        public virtual ICollection<TenantSubscription> Subscriptions { get; set; } = new List<TenantSubscription>();
        public virtual ICollection<UsageQuota> UsageQuotas { get; set; } = new List<UsageQuota>();
    }
}
