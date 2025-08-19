using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class TenantSubscription : BaseEntity
    {
        [Required]
        public new Guid TenantId { get; set; }
        
        [Required]
        public Guid BillingPlanId { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Cancelled, Suspended, PastDue, Trialing
        
        [Required]
        public DateTime StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }
        
        public DateTime? TrialEndDate { get; set; }
        
        public DateTime? CancelledAt { get; set; }
        
        [MaxLength(500)]
        public string? CancellationReason { get; set; }
        
        public bool AutoRenew { get; set; } = true;
        
        public DateTime NextBillingDate { get; set; }
        
        public decimal CurrentPrice { get; set; }
        
        [MaxLength(50)]
        public string? StripeSubscriptionId { get; set; }
        
        [MaxLength(50)]
        public string? StripeCustomerId { get; set; }
        
        [MaxLength(1000)]
        public string? Metadata { get; set; } // JSON for additional data
        
        public bool IsGrandfathered { get; set; } = false;
        
        public DateTime? LastPaymentDate { get; set; }
        
        public DateTime? NextPaymentAttempt { get; set; }
        
        public int FailedPaymentAttempts { get; set; } = 0;
        
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual BillingPlan BillingPlan { get; set; } = null!;
        public virtual ICollection<UsageRecord> UsageRecords { get; set; } = new List<UsageRecord>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
