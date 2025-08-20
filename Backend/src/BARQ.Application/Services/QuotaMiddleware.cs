using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BARQ.Application.Interfaces;
using BARQ.Infrastructure.Data;

namespace BARQ.Application.Services
{
    public class QuotaMiddleware : IQuotaMiddleware
    {
        private readonly BarqDbContext _context;
        private readonly ILogger<QuotaMiddleware> _logger;

        public QuotaMiddleware(BarqDbContext context, ILogger<QuotaMiddleware> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> CheckQuotaAsync(Guid tenantId, string quotaType, long requestedQuantity = 1)
        {
            try
            {
                var result = await ValidateQuotaAsync(tenantId, quotaType, requestedQuantity);
                return result.IsAllowed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking quota {QuotaType} for tenant {TenantId}", quotaType, tenantId);
                return true; // Allow on error to prevent blocking operations
            }
        }

        public async Task RecordUsageAsync(Guid tenantId, string quotaType, long quantity, string? entityId = null, string? entityType = null)
        {
            try
            {
                var quota = await _context.UsageQuotas
                    .FirstOrDefaultAsync(uq => uq.TenantId == tenantId && uq.QuotaType == quotaType && uq.IsActive);

                if (quota != null)
                {
                    quota.CurrentUsage += quantity;
                    quota.UpdatedAt = DateTime.UtcNow;
                }

                var usageRecord = new Core.Entities.UsageRecord
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    UsageType = quotaType,
                    Quantity = quantity,
                    RecordedAt = DateTime.UtcNow,
                    EntityId = entityId,
                    EntityType = entityType,
                    IsBillable = true,
                    BillingPeriod = DateTime.UtcNow.ToString("yyyy-MM"),
                    CreatedAt = DateTime.UtcNow
                };

                _context.UsageRecords.Add(usageRecord);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Recorded usage {QuotaType}: {Quantity} for tenant {TenantId}", quotaType, quantity, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording usage {QuotaType} for tenant {TenantId}", quotaType, tenantId);
            }
        }

        public async Task<QuotaCheckResult> ValidateQuotaAsync(Guid tenantId, string quotaType, long requestedQuantity = 1)
        {
            try
            {
                var quota = await _context.UsageQuotas
                    .FirstOrDefaultAsync(uq => uq.TenantId == tenantId && uq.QuotaType == quotaType && uq.IsActive);

                if (quota == null)
                {
                    return new QuotaCheckResult
                    {
                        IsAllowed = true,
                        IsNearLimit = false,
                        IsOverLimit = false,
                        RemainingQuota = long.MaxValue,
                        CurrentUsage = 0,
                        QuotaLimit = 0,
                        Message = "No quota limit configured",
                        RequiresUpgrade = false
                    };
                }

                if (quota.QuotaLimit == 0)
                {
                    return new QuotaCheckResult
                    {
                        IsAllowed = true,
                        IsNearLimit = false,
                        IsOverLimit = false,
                        RemainingQuota = long.MaxValue,
                        CurrentUsage = quota.CurrentUsage,
                        QuotaLimit = 0,
                        Message = "Unlimited quota",
                        RequiresUpgrade = false
                    };
                }

                var newUsage = quota.CurrentUsage + requestedQuantity;
                var usagePercentage = (double)quota.CurrentUsage / quota.QuotaLimit * 100;
                var newUsagePercentage = (double)newUsage / quota.QuotaLimit * 100;

                var isCurrentlyOverLimit = quota.CurrentUsage > quota.QuotaLimit;
                var wouldExceedLimit = newUsage > quota.QuotaLimit;
                var isNearLimit = usagePercentage >= 80;
                var remainingQuota = Math.Max(0, quota.QuotaLimit - quota.CurrentUsage);

                var result = new QuotaCheckResult
                {
                    IsAllowed = !quota.IsHardLimit || !wouldExceedLimit,
                    IsNearLimit = isNearLimit,
                    IsOverLimit = isCurrentlyOverLimit,
                    RemainingQuota = remainingQuota,
                    CurrentUsage = quota.CurrentUsage,
                    QuotaLimit = quota.QuotaLimit,
                    RequiresUpgrade = wouldExceedLimit && quota.IsHardLimit
                };

                if (wouldExceedLimit)
                {
                    if (quota.IsHardLimit)
                    {
                        result.Message = $"Quota exceeded. Current: {quota.CurrentUsage:N0}, Limit: {quota.QuotaLimit:N0}, Requested: {requestedQuantity:N0}";
                    }
                    else
                    {
                        result.Message = $"Quota exceeded but overage allowed. Current: {quota.CurrentUsage:N0}, Limit: {quota.QuotaLimit:N0}, Requested: {requestedQuantity:N0}";
                        if (quota.OverageRate.HasValue)
                        {
                            var overageAmount = (newUsage - quota.QuotaLimit) * quota.OverageRate.Value;
                            result.Message += $". Overage cost: ${overageAmount:F2}";
                        }
                    }
                }
                else if (isNearLimit)
                {
                    result.Message = $"Approaching quota limit. Current: {quota.CurrentUsage:N0}, Limit: {quota.QuotaLimit:N0} ({usagePercentage:F1}%)";
                }
                else
                {
                    result.Message = $"Within quota. Current: {quota.CurrentUsage:N0}, Limit: {quota.QuotaLimit:N0} ({usagePercentage:F1}%)";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating quota {QuotaType} for tenant {TenantId}", quotaType, tenantId);
                
                return new QuotaCheckResult
                {
                    IsAllowed = true, // Allow on error
                    IsNearLimit = false,
                    IsOverLimit = false,
                    RemainingQuota = 0,
                    CurrentUsage = 0,
                    QuotaLimit = 0,
                    Message = "Error checking quota - operation allowed",
                    RequiresUpgrade = false
                };
            }
        }
    }
}
