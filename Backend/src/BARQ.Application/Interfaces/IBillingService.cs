using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;

namespace BARQ.Application.Interfaces
{
    public interface IBillingService
    {
        Task<PagedResult<BillingPlanDto>> GetBillingPlansAsync(bool includeInactive = false);
        Task<BillingPlanDto?> GetBillingPlanAsync(Guid planId);
        Task<BillingPlanDto> CreateBillingPlanAsync(CreateBillingPlanRequest request);
        Task<BillingPlanDto?> UpdateBillingPlanAsync(Guid planId, UpdateBillingPlanRequest request);
        Task<bool> DeleteBillingPlanAsync(Guid planId);
        
        Task<TenantSubscriptionDto?> GetTenantSubscriptionAsync(Guid tenantId);
        Task<TenantSubscriptionDto> CreateSubscriptionAsync(Guid tenantId, Guid userId, CreateSubscriptionRequest request);
        Task<TenantSubscriptionDto?> UpdateSubscriptionAsync(Guid tenantId, UpdateSubscriptionRequest request);
        Task<bool> CancelSubscriptionAsync(Guid tenantId, CancelSubscriptionRequest request);
        Task<TenantSubscriptionDto?> UpgradeDowngradeAsync(Guid tenantId, UpgradeDowngradeRequest request);
        
        Task<PagedResult<UsageQuotaDto>> GetUsageQuotasAsync(Guid tenantId);
        Task<UsageQuotaDto?> GetUsageQuotaAsync(Guid tenantId, string quotaType);
        Task<bool> RecordUsageAsync(Guid tenantId, RecordUsageRequest request);
        Task<bool> CheckQuotaAsync(Guid tenantId, string quotaType, long requestedQuantity = 1);
        Task ResetQuotasAsync();
        
        Task<PagedResult<InvoiceDto>> GetInvoicesAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null);
        Task<InvoiceDto?> GetInvoiceAsync(Guid tenantId, Guid invoiceId);
        Task<Stream?> DownloadInvoiceAsync(Guid tenantId, Guid invoiceId);
        Task<InvoiceDto> GenerateInvoiceAsync(Guid tenantId, DateTime billingPeriodStart, DateTime billingPeriodEnd);
        
        Task<BillingDashboardDto> GetBillingDashboardAsync(Guid tenantId);
        Task<PagedResult<UsageRecordDto>> GetUsageHistoryAsync(Guid tenantId, string? usageType = null, DateTime? startDate = null, DateTime? endDate = null);
        
        Task ProcessSubscriptionBillingAsync();
        Task ProcessOverdueInvoicesAsync();
        Task SendUsageWarningsAsync();
    }
}
