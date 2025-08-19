namespace BARQ.Core.DTOs
{
    public class BillingPlanDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string BillingCycle { get; set; } = string.Empty;
        public string PlanType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsPublic { get; set; }
        public int MaxUsers { get; set; }
        public int MaxProjects { get; set; }
        public int MaxTasks { get; set; }
        public long MaxStorageBytes { get; set; }
        public int MaxAPICallsPerMonth { get; set; }
        public int MaxWorkflowExecutions { get; set; }
        public List<string> Features { get; set; } = new List<string>();
        public int TrialDays { get; set; }
        public bool RequiresCreditCard { get; set; }
        public int SortOrder { get; set; }
        public string? StripeProductId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateBillingPlanRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string BillingCycle { get; set; } = "Monthly";
        public string PlanType { get; set; } = "Standard";
        public bool IsPublic { get; set; } = true;
        public int MaxUsers { get; set; } = 0;
        public int MaxProjects { get; set; } = 0;
        public int MaxTasks { get; set; } = 0;
        public long MaxStorageBytes { get; set; } = 0;
        public int MaxAPICallsPerMonth { get; set; } = 0;
        public int MaxWorkflowExecutions { get; set; } = 0;
        public List<string> Features { get; set; } = new List<string>();
        public int TrialDays { get; set; } = 0;
        public bool RequiresCreditCard { get; set; } = false;
        public int SortOrder { get; set; } = 0;
    }

    public class UpdateBillingPlanRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? BillingCycle { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsPublic { get; set; }
        public int? MaxUsers { get; set; }
        public int? MaxProjects { get; set; }
        public int? MaxTasks { get; set; }
        public long? MaxStorageBytes { get; set; }
        public int? MaxAPICallsPerMonth { get; set; }
        public int? MaxWorkflowExecutions { get; set; }
        public List<string>? Features { get; set; }
        public int? TrialDays { get; set; }
        public bool? RequiresCreditCard { get; set; }
        public int? SortOrder { get; set; }
    }

    public class TenantSubscriptionDto
    {
        public string Id { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string BillingPlanId { get; set; } = string.Empty;
        public string BillingPlanName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? TrialEndDate { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public bool AutoRenew { get; set; }
        public DateTime NextBillingDate { get; set; }
        public decimal CurrentPrice { get; set; }
        public bool IsGrandfathered { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public DateTime? NextPaymentAttempt { get; set; }
        public int FailedPaymentAttempts { get; set; }
        public bool IsTrialing { get; set; }
        public int DaysUntilTrial { get; set; }
        public int DaysUntilBilling { get; set; }
        public BillingPlanDto? BillingPlan { get; set; }
    }

    public class CreateSubscriptionRequest
    {
        public string BillingPlanId { get; set; } = string.Empty;
        public bool StartTrial { get; set; } = false;
        public string? PaymentMethodId { get; set; }
        public string? CouponCode { get; set; }
    }

    public class UpdateSubscriptionRequest
    {
        public string? BillingPlanId { get; set; }
        public bool? AutoRenew { get; set; }
        public string? CancellationReason { get; set; }
    }

    public class CancelSubscriptionRequest
    {
        public string Reason { get; set; } = string.Empty;
        public bool CancelImmediately { get; set; } = false;
        public DateTime? CancelAt { get; set; }
    }

    public class UsageQuotaDto
    {
        public string Id { get; set; } = string.Empty;
        public string? TenantId { get; set; }
        public string? BillingPlanId { get; set; }
        public string QuotaType { get; set; } = string.Empty;
        public long QuotaLimit { get; set; }
        public long CurrentUsage { get; set; }
        public string ResetPeriod { get; set; } = string.Empty;
        public DateTime? LastResetDate { get; set; }
        public DateTime? NextResetDate { get; set; }
        public bool IsHardLimit { get; set; }
        public decimal? OverageRate { get; set; }
        public bool IsActive { get; set; }
        public string? Description { get; set; }
        public double UsagePercentage { get; set; }
        public bool IsNearLimit { get; set; }
        public bool IsOverLimit { get; set; }
        public long RemainingQuota { get; set; }
    }

    public class UsageRecordDto
    {
        public string Id { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string UsageType { get; set; } = string.Empty;
        public long Quantity { get; set; }
        public DateTime RecordedAt { get; set; }
        public string? EntityId { get; set; }
        public string? EntityType { get; set; }
        public decimal? Cost { get; set; }
        public bool IsBillable { get; set; }
        public string BillingPeriod { get; set; } = string.Empty;
        public bool IsProcessed { get; set; }
    }

    public class RecordUsageRequest
    {
        public string UsageType { get; set; } = string.Empty;
        public long Quantity { get; set; }
        public string? EntityId { get; set; }
        public string? EntityType { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class InvoiceDto
    {
        public string Id { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal SubtotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public DateTime BillingPeriodStart { get; set; }
        public DateTime BillingPeriodEnd { get; set; }
        public string? Notes { get; set; }
        public bool IsAutoGenerated { get; set; }
        public int PaymentAttempts { get; set; }
        public DateTime? LastPaymentAttempt { get; set; }
        public List<InvoiceLineItemDto> LineItems { get; set; } = new List<InvoiceLineItemDto>();
        public bool IsOverdue { get; set; }
        public int DaysOverdue { get; set; }
    }

    public class InvoiceLineItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public string? ItemType { get; set; }
        public DateTime? ServicePeriodStart { get; set; }
        public DateTime? ServicePeriodEnd { get; set; }
    }

    public class BillingDashboardDto
    {
        public TenantSubscriptionDto? CurrentSubscription { get; set; }
        public List<UsageQuotaDto> UsageQuotas { get; set; } = new List<UsageQuotaDto>();
        public List<InvoiceDto> RecentInvoices { get; set; } = new List<InvoiceDto>();
        public decimal MonthlySpend { get; set; }
        public decimal ProjectedSpend { get; set; }
        public List<UsageRecordDto> RecentUsage { get; set; } = new List<UsageRecordDto>();
        public bool HasPaymentMethod { get; set; }
        public DateTime? NextBillingDate { get; set; }
        public List<string> Alerts { get; set; } = new List<string>();
    }

    public class UpgradeDowngradeRequest
    {
        public string NewPlanId { get; set; } = string.Empty;
        public bool ProrateBilling { get; set; } = true;
        public DateTime? EffectiveDate { get; set; }
    }
}
