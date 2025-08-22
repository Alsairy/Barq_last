using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;
using BARQ.Core.Services;
using BARQ.Infrastructure.Data;
using System.Text.Json;

namespace BARQ.Application.Services
{
    public class BillingService : IBillingService
    {
        private readonly BarqDbContext _context;
        private readonly ILogger<BillingService> _logger;
        private readonly ITenantProvider _tenantProvider;

        public BillingService(BarqDbContext context, ILogger<BillingService> logger, ITenantProvider tenantProvider)
        {
            _context = context;
            _logger = logger;
            _tenantProvider = tenantProvider;
        }

        public async Task<PagedResult<BillingPlanDto>> GetBillingPlansAsync(bool includeInactive = false)
        {
            try
            {
                var query = _context.BillingPlans
                    .Where(bp => bp.TenantId == _tenantProvider.GetTenantId() && (includeInactive || bp.IsActive));

                var plans = await query
                    .OrderBy(bp => bp.SortOrder)
                    .ThenBy(bp => bp.Price)
                    .ToListAsync();

                var planDtos = plans.Select(MapToBillingPlanDto).ToList();

                return new PagedResult<BillingPlanDto>
                {
                    Items = planDtos,
                    Total = planDtos.Count,
                    Page = 1,
                    PageSize = planDtos.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving billing plans");
                throw;
            }
        }

        public async Task<BillingPlanDto?> GetBillingPlanAsync(Guid planId)
        {
            try
            {
                var plan = await _context.BillingPlans
                    .Where(bp => bp.TenantId == _tenantProvider.GetTenantId())
                    .FirstOrDefaultAsync(bp => bp.Id == planId);

                return plan != null ? MapToBillingPlanDto(plan) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving billing plan {PlanId}", planId);
                throw;
            }
        }

        public async Task<BillingPlanDto> CreateBillingPlanAsync(CreateBillingPlanRequest request)
        {
            try
            {
                var plan = new BillingPlan
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    BillingCycle = request.BillingCycle,
                    PlanType = request.PlanType,
                    IsActive = true,
                    IsPublic = request.IsPublic,
                    MaxUsers = request.MaxUsers,
                    MaxProjects = request.MaxProjects,
                    MaxTasks = request.MaxTasks,
                    MaxStorageBytes = request.MaxStorageBytes,
                    MaxAPICallsPerMonth = request.MaxAPICallsPerMonth,
                    MaxWorkflowExecutions = request.MaxWorkflowExecutions,
                    Features = JsonSerializer.Serialize(request.Features),
                    TrialDays = request.TrialDays,
                    RequiresCreditCard = request.RequiresCreditCard,
                    SortOrder = request.SortOrder,
                    CreatedAt = DateTime.UtcNow
                };

                _context.BillingPlans.Add(plan);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created billing plan {PlanId} - {PlanName}", plan.Id, plan.Name);

                return MapToBillingPlanDto(plan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating billing plan");
                throw;
            }
        }

        public async Task<BillingPlanDto?> UpdateBillingPlanAsync(Guid planId, UpdateBillingPlanRequest request)
        {
            try
            {
                var plan = await _context.BillingPlans
                    .Where(bp => bp.TenantId == _tenantProvider.GetTenantId())
                    .FirstOrDefaultAsync(bp => bp.Id == planId);

                if (plan == null)
                    throw new ArgumentException($"Billing plan with ID {planId} not found");

                if (request.Name != null) plan.Name = request.Name;
                if (request.Description != null) plan.Description = request.Description;
                if (request.Price.HasValue) plan.Price = request.Price.Value;
                if (request.BillingCycle != null) plan.BillingCycle = request.BillingCycle;
                if (request.IsActive.HasValue) plan.IsActive = request.IsActive.Value;
                if (request.IsPublic.HasValue) plan.IsPublic = request.IsPublic.Value;
                if (request.MaxUsers.HasValue) plan.MaxUsers = request.MaxUsers.Value;
                if (request.MaxProjects.HasValue) plan.MaxProjects = request.MaxProjects.Value;
                if (request.MaxTasks.HasValue) plan.MaxTasks = request.MaxTasks.Value;
                if (request.MaxStorageBytes.HasValue) plan.MaxStorageBytes = request.MaxStorageBytes.Value;
                if (request.MaxAPICallsPerMonth.HasValue) plan.MaxAPICallsPerMonth = request.MaxAPICallsPerMonth.Value;
                if (request.MaxWorkflowExecutions.HasValue) plan.MaxWorkflowExecutions = request.MaxWorkflowExecutions.Value;
                if (request.Features != null) plan.Features = JsonSerializer.Serialize(request.Features);
                if (request.TrialDays.HasValue) plan.TrialDays = request.TrialDays.Value;
                if (request.RequiresCreditCard.HasValue) plan.RequiresCreditCard = request.RequiresCreditCard.Value;
                if (request.SortOrder.HasValue) plan.SortOrder = request.SortOrder.Value;

                plan.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated billing plan {PlanId}", planId);

                return MapToBillingPlanDto(plan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating billing plan {PlanId}", planId);
                throw;
            }
        }

        public async Task<bool> DeleteBillingPlanAsync(Guid planId)
        {
            try
            {
                var plan = await _context.BillingPlans
                    .Where(bp => bp.TenantId == _tenantProvider.GetTenantId())
                    .FirstOrDefaultAsync(bp => bp.Id == planId);

                if (plan == null)
                    return false;

                var hasActiveSubscriptions = await _context.TenantSubscriptions
                    .AnyAsync(ts => ts.BillingPlanId == planId && ts.Status == "Active");

                if (hasActiveSubscriptions)
                {
                    plan.IsActive = false;
                    plan.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _context.BillingPlans.Remove(plan);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted/deactivated billing plan {PlanId}", planId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting billing plan {PlanId}", planId);
                throw;
            }
        }

        public async Task<TenantSubscriptionDto?> GetTenantSubscriptionAsync(Guid tenantId)
        {
            try
            {
                var subscription = await _context.TenantSubscriptions
                    .Include(ts => ts.BillingPlan)
                    .Include(ts => ts.Tenant)
                    .FirstOrDefaultAsync(ts => ts.TenantId == tenantId && ts.Status != "Cancelled");

                return subscription != null ? MapToTenantSubscriptionDto(subscription) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenant subscription for {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<TenantSubscriptionDto> CreateSubscriptionAsync(Guid tenantId, Guid userId, CreateSubscriptionRequest request)
        {
            try
            {
                var plan = await _context.BillingPlans
                    .Where(bp => bp.TenantId == _tenantProvider.GetTenantId())
                    .FirstOrDefaultAsync(bp => bp.Id == Guid.Parse(request.BillingPlanId));

                if (plan == null)
                    throw new ArgumentException("Billing plan not found");

                var existingSubscription = await _context.TenantSubscriptions
                    .FirstOrDefaultAsync(ts => ts.TenantId == tenantId && ts.Status == "Active");

                if (existingSubscription != null)
                    throw new InvalidOperationException("Tenant already has an active subscription");

                var startDate = DateTime.UtcNow;
                var trialEndDate = request.StartTrial && plan.TrialDays > 0 
                    ? startDate.AddDays(plan.TrialDays) 
                    : (DateTime?)null;

                var subscription = new TenantSubscription
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    BillingPlanId = plan.Id,
                    Status = request.StartTrial ? "Trialing" : "Active",
                    StartDate = startDate,
                    TrialEndDate = trialEndDate,
                    AutoRenew = true,
                    NextBillingDate = trialEndDate ?? GetNextBillingDate(startDate, plan.BillingCycle),
                    CurrentPrice = plan.Price,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TenantSubscriptions.Add(subscription);

                await CreateInitialQuotasAsync(tenantId, plan);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Created subscription {SubscriptionId} for tenant {TenantId}", subscription.Id, tenantId);

                subscription.BillingPlan = plan;
                return MapToTenantSubscriptionDto(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<TenantSubscriptionDto?> UpdateSubscriptionAsync(Guid tenantId, UpdateSubscriptionRequest request)
        {
            try
            {
                var subscription = await _context.TenantSubscriptions
                    .Include(ts => ts.BillingPlan)
                    .FirstOrDefaultAsync(ts => ts.TenantId == tenantId && ts.Status != "Cancelled");

                if (subscription == null)
                    throw new ArgumentException($"Subscription not found for tenant {tenantId}");

                if (request.BillingPlanId != null)
                {
                    var newPlan = await _context.BillingPlans
                        .Where(bp => bp.TenantId == _tenantProvider.GetTenantId())
                        .FirstOrDefaultAsync(bp => bp.Id == Guid.Parse(request.BillingPlanId));

                    if (newPlan != null)
                    {
                        subscription.BillingPlanId = newPlan.Id;
                        subscription.CurrentPrice = newPlan.Price;
                        subscription.NextBillingDate = GetNextBillingDate(DateTime.UtcNow, newPlan.BillingCycle);
                        
                        await UpdateQuotasForPlanChange(tenantId, newPlan);
                    }
                }

                if (request.AutoRenew.HasValue)
                    subscription.AutoRenew = request.AutoRenew.Value;

                subscription.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated subscription for tenant {TenantId}", tenantId);

                return MapToTenantSubscriptionDto(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<bool> CancelSubscriptionAsync(Guid tenantId, CancelSubscriptionRequest request)
        {
            try
            {
                var subscription = await _context.TenantSubscriptions
                    .FirstOrDefaultAsync(ts => ts.TenantId == tenantId && ts.Status != "Cancelled");

                if (subscription == null)
                    return false;

                subscription.Status = "Cancelled";
                subscription.CancelledAt = request.CancelImmediately ? DateTime.UtcNow : (request.CancelAt ?? subscription.NextBillingDate);
                subscription.CancellationReason = request.Reason;
                subscription.AutoRenew = false;
                subscription.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Cancelled subscription for tenant {TenantId}", tenantId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling subscription for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<TenantSubscriptionDto?> UpgradeDowngradeAsync(Guid tenantId, UpgradeDowngradeRequest request)
        {
            try
            {
                var subscription = await _context.TenantSubscriptions
                    .Include(ts => ts.BillingPlan)
                    .FirstOrDefaultAsync(ts => ts.TenantId == tenantId && ts.Status == "Active");

                if (subscription == null)
                    throw new ArgumentException($"Active subscription not found for tenant {tenantId}");

                var newPlan = await _context.BillingPlans
                    .Where(bp => bp.TenantId == _tenantProvider.GetTenantId())
                    .FirstOrDefaultAsync(bp => bp.Id == Guid.Parse(request.NewPlanId));

                if (newPlan == null)
                    throw new ArgumentException("New billing plan not found");

                var effectiveDate = request.EffectiveDate ?? DateTime.UtcNow;

                if (request.ProrateBilling && effectiveDate <= DateTime.UtcNow)
                {
                    await GenerateProrationInvoice(subscription, newPlan, effectiveDate);
                }

                subscription.BillingPlanId = newPlan.Id;
                subscription.CurrentPrice = newPlan.Price;
                subscription.NextBillingDate = GetNextBillingDate(effectiveDate, newPlan.BillingCycle);
                subscription.UpdatedAt = DateTime.UtcNow;

                await UpdateQuotasForPlanChange(tenantId, newPlan);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Upgraded/downgraded subscription for tenant {TenantId} to plan {PlanId}", tenantId, newPlan.Id);

                subscription.BillingPlan = newPlan;
                return MapToTenantSubscriptionDto(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upgrading/downgrading subscription for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<PagedResult<UsageQuotaDto>> GetUsageQuotasAsync(Guid tenantId)
        {
            try
            {
                var quotas = await _context.UsageQuotas
                    .Where(uq => uq.TenantId == tenantId && uq.IsActive)
                    .OrderBy(uq => uq.QuotaType)
                    .ToListAsync();

                var quotaDtos = quotas.Select(MapToUsageQuotaDto).ToList();

                return new PagedResult<UsageQuotaDto>
                {
                    Items = quotaDtos,
                    Total = quotaDtos.Count,
                    Page = 1,
                    PageSize = quotaDtos.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving usage quotas for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<UsageQuotaDto?> GetUsageQuotaAsync(Guid tenantId, string quotaType)
        {
            try
            {
                var quota = await _context.UsageQuotas
                    .FirstOrDefaultAsync(uq => uq.TenantId == tenantId && uq.QuotaType == quotaType && uq.IsActive);

                return quota != null ? MapToUsageQuotaDto(quota) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving usage quota {QuotaType} for tenant {TenantId}", quotaType, tenantId);
                throw;
            }
        }

        public async Task<bool> RecordUsageAsync(Guid tenantId, RecordUsageRequest request)
        {
            try
            {
                var quota = await _context.UsageQuotas
                    .FirstOrDefaultAsync(uq => uq.TenantId == tenantId && uq.QuotaType == request.UsageType && uq.IsActive);

                if (quota != null)
                {
                    quota.CurrentUsage += request.Quantity;
                    quota.UpdatedAt = DateTime.UtcNow;
                }

                var usageRecord = new UsageRecord
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    UsageType = request.UsageType,
                    Quantity = request.Quantity,
                    RecordedAt = DateTime.UtcNow,
                    EntityId = request.EntityId,
                    EntityType = request.EntityType,
                    Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null,
                    IsBillable = true,
                    BillingPeriod = DateTime.UtcNow.ToString("yyyy-MM"),
                    CreatedAt = DateTime.UtcNow
                };

                _context.UsageRecords.Add(usageRecord);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Recorded usage {UsageType}: {Quantity} for tenant {TenantId}", request.UsageType, request.Quantity, tenantId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording usage for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<bool> CheckQuotaAsync(Guid tenantId, string quotaType, long requestedQuantity = 1)
        {
            try
            {
                var quota = await _context.UsageQuotas
                    .FirstOrDefaultAsync(uq => uq.TenantId == tenantId && uq.QuotaType == quotaType && uq.IsActive);

                if (quota == null)
                    return true; // No quota means unlimited

                if (quota.QuotaLimit == 0)
                    return true; // 0 means unlimited

                return (quota.CurrentUsage + requestedQuantity) <= quota.QuotaLimit;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking quota {QuotaType} for tenant {TenantId}", quotaType, tenantId);
                throw;
            }
        }

        public async System.Threading.Tasks.Task ResetQuotasAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var quotasToReset = await _context.UsageQuotas
                    .Where(uq => uq.IsActive && uq.NextResetDate <= now)
                    .ToListAsync();

                foreach (var quota in quotasToReset)
                {
                    quota.CurrentUsage = 0;
                    quota.LastResetDate = now;
                    quota.NextResetDate = CalculateNextResetDate(now, quota.ResetPeriod);
                    quota.UpdatedAt = now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Reset {Count} quotas", quotasToReset.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting quotas");
                throw;
            }
        }

        public async Task<PagedResult<InvoiceDto>> GetInvoicesAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.Invoices
                    .Include(i => i.LineItems)
                    .Where(i => i.TenantId == tenantId && 
                               (!startDate.HasValue || i.InvoiceDate >= startDate.Value) &&
                               (!endDate.HasValue || i.InvoiceDate <= endDate.Value));

                var invoices = await query
                    .OrderByDescending(i => i.InvoiceDate)
                    .ToListAsync();

                var invoiceDtos = invoices.Select(MapToInvoiceDto).ToList();

                return new PagedResult<InvoiceDto>
                {
                    Items = invoiceDtos,
                    Total = invoiceDtos.Count,
                    Page = 1,
                    PageSize = invoiceDtos.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<InvoiceDto?> GetInvoiceAsync(Guid tenantId, Guid invoiceId)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.LineItems)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId);

                return invoice != null ? MapToInvoiceDto(invoice) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice {InvoiceId} for tenant {TenantId}", invoiceId, tenantId);
                throw;
            }
        }

        public async Task<Stream?> DownloadInvoiceAsync(Guid tenantId, Guid invoiceId)
        {
            try
            {
                var invoice = await GetInvoiceAsync(tenantId, invoiceId);
                if (invoice == null)
                    throw new ArgumentException($"Invoice {invoiceId} not found for tenant {tenantId}");

                var content = $"Invoice {invoice.InvoiceNumber} - ${invoice.TotalAmount}";
                var bytes = System.Text.Encoding.UTF8.GetBytes(content);
                return new MemoryStream(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading invoice {InvoiceId} for tenant {TenantId}", invoiceId, tenantId);
                throw;
            }
        }

        public async Task<InvoiceDto> GenerateInvoiceAsync(Guid tenantId, DateTime billingPeriodStart, DateTime billingPeriodEnd)
        {
            try
            {
                var subscription = await _context.TenantSubscriptions
                    .Include(ts => ts.BillingPlan)
                    .FirstOrDefaultAsync(ts => ts.TenantId == tenantId && ts.Status == "Active");

                if (subscription == null)
                    throw new InvalidOperationException("No active subscription found");

                var invoiceNumber = await GenerateInvoiceNumberAsync();

                var invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SubscriptionId = subscription.Id,
                    InvoiceNumber = invoiceNumber,
                    Status = "Draft",
                    InvoiceDate = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(30),
                    BillingPeriodStart = billingPeriodStart,
                    BillingPeriodEnd = billingPeriodEnd,
                    Currency = "USD",
                    IsAutoGenerated = true,
                    CreatedAt = DateTime.UtcNow
                };

                var subscriptionLineItem = new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    Description = $"{subscription.BillingPlan.Name} - {billingPeriodStart:MMM yyyy}",
                    Quantity = 1,
                    UnitPrice = subscription.CurrentPrice,
                    Amount = subscription.CurrentPrice,
                    ItemType = "Subscription",
                    ServicePeriodStart = billingPeriodStart,
                    ServicePeriodEnd = billingPeriodEnd,
                    CreatedAt = DateTime.UtcNow
                };

                invoice.LineItems.Add(subscriptionLineItem);

                await AddUsageCharges(invoice, tenantId, billingPeriodStart, billingPeriodEnd);

                invoice.SubtotalAmount = invoice.LineItems.Sum(li => li.Amount);
                invoice.TotalAmount = invoice.SubtotalAmount + invoice.TaxAmount - invoice.DiscountAmount;

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Generated invoice {InvoiceNumber} for tenant {TenantId}", invoiceNumber, tenantId);

                return MapToInvoiceDto(invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<BillingDashboardDto> GetBillingDashboardAsync(Guid tenantId)
        {
            try
            {
                var subscription = await GetTenantSubscriptionAsync(tenantId);
                var quotas = await GetUsageQuotasAsync(tenantId);
                var recentInvoices = await GetInvoicesAsync(tenantId, DateTime.UtcNow.AddMonths(-3));
                var recentUsage = await GetUsageHistoryAsync(tenantId, null, DateTime.UtcNow.AddDays(-30));

                var monthlySpend = recentInvoices.Items
                    .Where(i => i.InvoiceDate >= DateTime.UtcNow.AddMonths(-1))
                    .Sum(i => i.TotalAmount);

                var alerts = new List<string>();
                foreach (var quota in quotas.Items)
                {
                    if (quota.IsOverLimit)
                        alerts.Add($"{quota.QuotaType} quota exceeded");
                    else if (quota.IsNearLimit)
                        alerts.Add($"{quota.QuotaType} quota at {quota.UsagePercentage:F0}%");
                }

                return new BillingDashboardDto
                {
                    CurrentSubscription = subscription,
                    UsageQuotas = quotas.Items.ToList(),
                    RecentInvoices = recentInvoices.Items.Take(5).ToList(),
                    MonthlySpend = monthlySpend,
                    ProjectedSpend = monthlySpend * 1.1m, // Simple projection
                    RecentUsage = recentUsage.Items.Take(10).ToList(),
                    HasPaymentMethod = !string.IsNullOrEmpty(subscription?.BillingPlan?.StripeProductId),
                    NextBillingDate = subscription?.NextBillingDate,
                    Alerts = alerts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving billing dashboard for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<PagedResult<UsageRecordDto>> GetUsageHistoryAsync(Guid tenantId, string? usageType = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.UsageRecords
                    .Where(ur => ur.TenantId == tenantId &&
                                (string.IsNullOrEmpty(usageType) || ur.UsageType == usageType) &&
                                (!startDate.HasValue || ur.RecordedAt >= startDate.Value) &&
                                (!endDate.HasValue || ur.RecordedAt <= endDate.Value));

                var records = await query
                    .OrderByDescending(ur => ur.RecordedAt)
                    .Take(100)
                    .ToListAsync();

                var recordDtos = records.Select(MapToUsageRecordDto).ToList();

                return new PagedResult<UsageRecordDto>
                {
                    Items = recordDtos,
                    Total = recordDtos.Count,
                    Page = 1,
                    PageSize = recordDtos.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving usage history for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async System.Threading.Tasks.Task ProcessSubscriptionBillingAsync()
        {
            try
            {
                var subscriptionsDue = await _context.TenantSubscriptions
                    .Include(ts => ts.BillingPlan)
                    .Where(ts => ts.TenantId == _tenantProvider.GetTenantId() && ts.Status == "Active" && ts.NextBillingDate <= DateTime.UtcNow)
                    .ToListAsync();

                foreach (var subscription in subscriptionsDue)
                {
                    try
                    {
                        var billingPeriodStart = subscription.NextBillingDate;
                        var billingPeriodEnd = GetNextBillingDate(billingPeriodStart, subscription.BillingPlan.BillingCycle);

                        await GenerateInvoiceAsync(subscription.TenantId, billingPeriodStart, billingPeriodEnd.AddDays(-1));

                        subscription.NextBillingDate = billingPeriodEnd;
                        subscription.LastPaymentDate = DateTime.UtcNow;
                        subscription.UpdatedAt = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing billing for subscription {SubscriptionId}", subscription.Id);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Processed billing for {Count} subscriptions", subscriptionsDue.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing subscription billing");
                throw;
            }
        }

        public async System.Threading.Tasks.Task ProcessOverdueInvoicesAsync()
        {
            try
            {
                var overdueInvoices = await _context.Invoices
                    .Where(i => i.TenantId == _tenantProvider.GetTenantId() && i.Status == "Sent" && i.DueDate < DateTime.UtcNow)
                    .ToListAsync();

                foreach (var invoice in overdueInvoices)
                {
                    invoice.Status = "Overdue";
                    invoice.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Marked {Count} invoices as overdue", overdueInvoices.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing overdue invoices");
                throw;
            }
        }

        public async System.Threading.Tasks.Task SendUsageWarningsAsync()
        {
            try
            {
                var quotasNearLimit = await _context.UsageQuotas
                    .Where(uq => uq.IsActive && uq.QuotaLimit > 0 && 
                                (uq.CurrentUsage * 100.0 / uq.QuotaLimit) >= 80)
                    .ToListAsync();

                foreach (var quota in quotasNearLimit)
                {
                    var usagePercentage = (double)quota.CurrentUsage / quota.QuotaLimit * 100;
                    
                    if (usagePercentage >= 95 && quota.SendWarningAt95Percent)
                    {
                        _logger.LogWarning("Quota {QuotaType} for tenant {TenantId} is at {Percentage:F1}%", 
                            quota.QuotaType, quota.TenantId, usagePercentage);
                    }
                    else if (usagePercentage >= 80 && quota.SendWarningAt80Percent)
                    {
                        _logger.LogInformation("Quota {QuotaType} for tenant {TenantId} is at {Percentage:F1}%", 
                            quota.QuotaType, quota.TenantId, usagePercentage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending usage warnings");
                throw;
            }
        }

        private static BillingPlanDto MapToBillingPlanDto(BillingPlan plan)
        {
            var features = new List<string>();
            if (!string.IsNullOrEmpty(plan.Features))
            {
                try
                {
                    features = JsonSerializer.Deserialize<List<string>>(plan.Features) ?? new List<string>();
                }
                catch
                {
                }
            }

            return new BillingPlanDto
            {
                Id = plan.Id.ToString(),
                Name = plan.Name,
                Description = plan.Description,
                Price = plan.Price,
                BillingCycle = plan.BillingCycle,
                PlanType = plan.PlanType,
                IsActive = plan.IsActive,
                IsPublic = plan.IsPublic,
                MaxUsers = plan.MaxUsers,
                MaxProjects = plan.MaxProjects,
                MaxTasks = plan.MaxTasks,
                MaxStorageBytes = plan.MaxStorageBytes,
                MaxAPICallsPerMonth = plan.MaxAPICallsPerMonth,
                MaxWorkflowExecutions = plan.MaxWorkflowExecutions,
                Features = features,
                TrialDays = plan.TrialDays,
                RequiresCreditCard = plan.RequiresCreditCard,
                SortOrder = plan.SortOrder,
                CreatedAt = plan.CreatedAt,
                UpdatedAt = plan.UpdatedAt
            };
        }

        private static TenantSubscriptionDto MapToTenantSubscriptionDto(TenantSubscription subscription)
        {
            var isTrialing = subscription.Status == "Trialing" && subscription.TrialEndDate > DateTime.UtcNow;
            var daysUntilTrial = isTrialing && subscription.TrialEndDate.HasValue 
                ? (int)(subscription.TrialEndDate.Value - DateTime.UtcNow).TotalDays 
                : 0;
            var daysUntilBilling = (int)(subscription.NextBillingDate - DateTime.UtcNow).TotalDays;

            return new TenantSubscriptionDto
            {
                Id = subscription.Id.ToString(),
                TenantId = subscription.TenantId.ToString(),
                TenantName = subscription.Tenant?.Name ?? "",
                BillingPlanId = subscription.BillingPlanId.ToString(),
                BillingPlanName = subscription.BillingPlan?.Name ?? "",
                Status = subscription.Status,
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                TrialEndDate = subscription.TrialEndDate,
                CancelledAt = subscription.CancelledAt,
                CancellationReason = subscription.CancellationReason,
                AutoRenew = subscription.AutoRenew,
                NextBillingDate = subscription.NextBillingDate,
                CurrentPrice = subscription.CurrentPrice,
                IsGrandfathered = subscription.IsGrandfathered,
                LastPaymentDate = subscription.LastPaymentDate,
                NextPaymentAttempt = subscription.NextPaymentAttempt,
                FailedPaymentAttempts = subscription.FailedPaymentAttempts,
                IsTrialing = isTrialing,
                DaysUntilTrial = daysUntilTrial,
                DaysUntilBilling = daysUntilBilling,
                BillingPlan = subscription.BillingPlan != null ? MapToBillingPlanDto(subscription.BillingPlan) : null
            };
        }

        private static UsageQuotaDto MapToUsageQuotaDto(UsageQuota quota)
        {
            var usagePercentage = quota.QuotaLimit > 0 ? (double)quota.CurrentUsage / quota.QuotaLimit * 100 : 0;
            var isNearLimit = usagePercentage >= 80;
            var isOverLimit = quota.CurrentUsage > quota.QuotaLimit && quota.QuotaLimit > 0;
            var remainingQuota = Math.Max(0, quota.QuotaLimit - quota.CurrentUsage);

            return new UsageQuotaDto
            {
                Id = quota.Id.ToString(),
                TenantId = quota.TenantId?.ToString(),
                BillingPlanId = quota.BillingPlanId?.ToString(),
                QuotaType = quota.QuotaType,
                QuotaLimit = quota.QuotaLimit,
                CurrentUsage = quota.CurrentUsage,
                ResetPeriod = quota.ResetPeriod,
                LastResetDate = quota.LastResetDate,
                NextResetDate = quota.NextResetDate,
                IsHardLimit = quota.IsHardLimit,
                OverageRate = quota.OverageRate,
                IsActive = quota.IsActive,
                Description = quota.Description,
                UsagePercentage = usagePercentage,
                IsNearLimit = isNearLimit,
                IsOverLimit = isOverLimit,
                RemainingQuota = remainingQuota
            };
        }

        private static UsageRecordDto MapToUsageRecordDto(UsageRecord record)
        {
            return new UsageRecordDto
            {
                Id = record.Id.ToString(),
                TenantId = record.TenantId.ToString(),
                UsageType = record.UsageType,
                Quantity = record.Quantity,
                RecordedAt = record.RecordedAt,
                EntityId = record.EntityId,
                EntityType = record.EntityType,
                Cost = record.Cost,
                IsBillable = record.IsBillable,
                BillingPeriod = record.BillingPeriod,
                IsProcessed = record.IsProcessed
            };
        }

        private static InvoiceDto MapToInvoiceDto(Invoice invoice)
        {
            var isOverdue = invoice.Status == "Sent" && invoice.DueDate < DateTime.UtcNow;
            var daysOverdue = isOverdue ? (int)(DateTime.UtcNow - invoice.DueDate).TotalDays : 0;

            return new InvoiceDto
            {
                Id = invoice.Id.ToString(),
                TenantId = invoice.TenantId.ToString(),
                InvoiceNumber = invoice.InvoiceNumber,
                Status = invoice.Status,
                SubtotalAmount = invoice.SubtotalAmount,
                TaxAmount = invoice.TaxAmount,
                DiscountAmount = invoice.DiscountAmount,
                TotalAmount = invoice.TotalAmount,
                Currency = invoice.Currency,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                PaidDate = invoice.PaidDate,
                BillingPeriodStart = invoice.BillingPeriodStart,
                BillingPeriodEnd = invoice.BillingPeriodEnd,
                Notes = invoice.Notes,
                IsAutoGenerated = invoice.IsAutoGenerated,
                PaymentAttempts = invoice.PaymentAttempts,
                LastPaymentAttempt = invoice.LastPaymentAttempt,
                LineItems = invoice.LineItems.Select(MapToInvoiceLineItemDto).ToList(),
                IsOverdue = isOverdue,
                DaysOverdue = daysOverdue
            };
        }

        private static InvoiceLineItemDto MapToInvoiceLineItemDto(InvoiceLineItem lineItem)
        {
            return new InvoiceLineItemDto
            {
                Id = lineItem.Id.ToString(),
                Description = lineItem.Description,
                Quantity = lineItem.Quantity,
                UnitPrice = lineItem.UnitPrice,
                Amount = lineItem.Amount,
                ItemType = lineItem.ItemType,
                ServicePeriodStart = lineItem.ServicePeriodStart,
                ServicePeriodEnd = lineItem.ServicePeriodEnd
            };
        }

        private static DateTime GetNextBillingDate(DateTime startDate, string billingCycle)
        {
            return billingCycle.ToLower() switch
            {
                "monthly" => startDate.AddMonths(1),
                "yearly" => startDate.AddYears(1),
                "weekly" => startDate.AddDays(7),
                "daily" => startDate.AddDays(1),
                _ => startDate.AddMonths(1)
            };
        }

        private static DateTime CalculateNextResetDate(DateTime currentDate, string resetPeriod)
        {
            return resetPeriod.ToLower() switch
            {
                "daily" => currentDate.AddDays(1),
                "weekly" => currentDate.AddDays(7),
                "monthly" => currentDate.AddMonths(1),
                "yearly" => currentDate.AddYears(1),
                _ => currentDate.AddMonths(1)
            };
        }

        private async System.Threading.Tasks.Task CreateInitialQuotasAsync(Guid tenantId, BillingPlan plan)
        {
            var quotaTypes = new[]
            {
                ("Users", (long)plan.MaxUsers),
                ("Projects", (long)plan.MaxProjects),
                ("Tasks", (long)plan.MaxTasks),
                ("Storage", plan.MaxStorageBytes),
                ("APICall", (long)plan.MaxAPICallsPerMonth),
                ("WorkflowExecution", (long)plan.MaxWorkflowExecutions)
            };

            foreach (var (quotaType, limit) in quotaTypes)
            {
                if (limit > 0)
                {
                    var quota = new UsageQuota
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        BillingPlanId = plan.Id,
                        QuotaType = quotaType,
                        QuotaLimit = limit,
                        CurrentUsage = 0,
                        ResetPeriod = quotaType == "APICall" ? "Monthly" : "Never",
                        NextResetDate = quotaType == "APICall" ? DateTime.UtcNow.AddMonths(1) : null,
                        IsHardLimit = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.UsageQuotas.Add(quota);
                }
            }
        }

        private async System.Threading.Tasks.Task UpdateQuotasForPlanChange(Guid tenantId, BillingPlan newPlan)
        {
            var existingQuotas = await _context.UsageQuotas
                .Where(uq => uq.TenantId == tenantId)
                .ToListAsync();

            var quotaUpdates = new Dictionary<string, long>
            {
                ["Users"] = newPlan.MaxUsers,
                ["Projects"] = newPlan.MaxProjects,
                ["Tasks"] = newPlan.MaxTasks,
                ["Storage"] = newPlan.MaxStorageBytes,
                ["APICall"] = newPlan.MaxAPICallsPerMonth,
                ["WorkflowExecution"] = newPlan.MaxWorkflowExecutions
            };

            foreach (var (quotaType, newLimit) in quotaUpdates)
            {
                var existingQuota = existingQuotas.FirstOrDefault(q => q.QuotaType == quotaType);
                if (existingQuota != null)
                {
                    existingQuota.QuotaLimit = newLimit;
                    existingQuota.BillingPlanId = newPlan.Id;
                    existingQuota.UpdatedAt = DateTime.UtcNow;
                }
                else if (newLimit > 0)
                {
                    var newQuota = new UsageQuota
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        BillingPlanId = newPlan.Id,
                        QuotaType = quotaType,
                        QuotaLimit = newLimit,
                        CurrentUsage = 0,
                        ResetPeriod = quotaType == "APICall" ? "Monthly" : "Never",
                        NextResetDate = quotaType == "APICall" ? DateTime.UtcNow.AddMonths(1) : null,
                        IsHardLimit = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.UsageQuotas.Add(newQuota);
                }
            }
        }

        private async System.Threading.Tasks.Task GenerateProrationInvoice(TenantSubscription subscription, BillingPlan newPlan, DateTime effectiveDate)
        {
            var daysInCurrentPeriod = (subscription.NextBillingDate - subscription.StartDate).TotalDays;
            var daysRemaining = (subscription.NextBillingDate - effectiveDate).TotalDays;
            var prorationFactor = daysRemaining / daysInCurrentPeriod;

            var currentPlanCredit = subscription.CurrentPrice * (decimal)prorationFactor;
            var newPlanCharge = newPlan.Price * (decimal)prorationFactor;
            var prorationAmount = newPlanCharge - currentPlanCredit;

            if (Math.Abs(prorationAmount) > 0.01m)
            {
                var invoiceNumber = await GenerateInvoiceNumberAsync();

                var prorationInvoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    TenantId = subscription.TenantId,
                    SubscriptionId = subscription.Id,
                    InvoiceNumber = invoiceNumber,
                    Status = "Draft",
                    InvoiceDate = effectiveDate,
                    DueDate = effectiveDate.AddDays(1),
                    BillingPeriodStart = effectiveDate,
                    BillingPeriodEnd = subscription.NextBillingDate,
                    Currency = "USD",
                    SubtotalAmount = prorationAmount,
                    TotalAmount = prorationAmount,
                    IsAutoGenerated = true,
                    CreatedAt = DateTime.UtcNow
                };

                var lineItem = new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = prorationInvoice.Id,
                    Description = $"Plan change proration: {subscription.BillingPlan?.Name} → {newPlan.Name}",
                    Quantity = 1,
                    UnitPrice = prorationAmount,
                    Amount = prorationAmount,
                    ItemType = "Proration",
                    ServicePeriodStart = effectiveDate,
                    ServicePeriodEnd = subscription.NextBillingDate,
                    CreatedAt = DateTime.UtcNow
                };

                prorationInvoice.LineItems.Add(lineItem);
                _context.Invoices.Add(prorationInvoice);
            }
        }

        private async System.Threading.Tasks.Task AddUsageCharges(Invoice invoice, Guid tenantId, DateTime billingPeriodStart, DateTime billingPeriodEnd)
        {
            var usageRecords = await _context.UsageRecords
                .Where(ur => ur.TenantId == tenantId && 
                           ur.RecordedAt >= billingPeriodStart && 
                           ur.RecordedAt < billingPeriodEnd &&
                           ur.IsBillable && !ur.IsProcessed)
                .GroupBy(ur => ur.UsageType)
                .Select(g => new { UsageType = g.Key, TotalQuantity = g.Sum(ur => ur.Quantity) })
                .ToListAsync();

            var quotas = await _context.UsageQuotas
                .Where(uq => uq.TenantId == tenantId && uq.OverageRate.HasValue)
                .ToListAsync();

            foreach (var usage in usageRecords)
            {
                var quota = quotas.FirstOrDefault(q => q.QuotaType == usage.UsageType);
                if (quota?.OverageRate.HasValue == true)
                {
                    var overageQuantity = Math.Max(0, usage.TotalQuantity - quota.QuotaLimit);
                    if (overageQuantity > 0)
                    {
                        var overageAmount = overageQuantity * quota.OverageRate.Value;

                        var lineItem = new InvoiceLineItem
                        {
                            Id = Guid.NewGuid(),
                            InvoiceId = invoice.Id,
                            Description = $"{usage.UsageType} overage ({overageQuantity:N0} units)",
                            Quantity = overageQuantity,
                            UnitPrice = quota.OverageRate.Value,
                            Amount = overageAmount,
                            ItemType = "Usage",
                            ServicePeriodStart = billingPeriodStart,
                            ServicePeriodEnd = billingPeriodEnd,
                            CreatedAt = DateTime.UtcNow
                        };

                        invoice.LineItems.Add(lineItem);
                    }
                }
            }

            var recordsToUpdate = await _context.UsageRecords
                .Where(ur => ur.TenantId == tenantId && 
                           ur.RecordedAt >= billingPeriodStart && 
                           ur.RecordedAt < billingPeriodEnd &&
                           ur.IsBillable && !ur.IsProcessed)
                .ToListAsync();

            foreach (var record in recordsToUpdate)
            {
                record.IsProcessed = true;
                record.UpdatedAt = DateTime.UtcNow;
            }
        }

        private async Task<string> GenerateInvoiceNumberAsync()
        {
            var year = DateTime.UtcNow.Year;
            var month = DateTime.UtcNow.Month;

            var lastInvoice = await _context.Invoices
                .Where(i => i.TenantId == _tenantProvider.GetTenantId() && i.InvoiceNumber.StartsWith($"INV-{year:D4}-{month:D2}"))
                .OrderByDescending(i => i.InvoiceNumber)
                .FirstOrDefaultAsync();

            var sequence = 1;
            if (lastInvoice != null)
            {
                var parts = lastInvoice.InvoiceNumber.Split('-');
                if (parts.Length == 4 && int.TryParse(parts[3], out var lastSequence))
                {
                    sequence = lastSequence + 1;
                }
            }

            return $"INV-{year:D4}-{month:D2}-{sequence:D4}";
        }
    }
}
