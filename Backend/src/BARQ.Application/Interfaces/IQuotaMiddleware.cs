namespace BARQ.Application.Interfaces
{
    public interface IQuotaMiddleware
    {
        Task<bool> CheckQuotaAsync(Guid tenantId, string quotaType, long requestedQuantity = 1);
        Task RecordUsageAsync(Guid tenantId, string quotaType, long quantity, string? entityId = null, string? entityType = null);
        Task<QuotaCheckResult> ValidateQuotaAsync(Guid tenantId, string quotaType, long requestedQuantity = 1);
    }

    public class QuotaCheckResult
    {
        public bool IsAllowed { get; set; }
        public bool IsNearLimit { get; set; }
        public bool IsOverLimit { get; set; }
        public long RemainingQuota { get; set; }
        public long CurrentUsage { get; set; }
        public long QuotaLimit { get; set; }
        public string? Message { get; set; }
        public bool RequiresUpgrade { get; set; }
    }
}
