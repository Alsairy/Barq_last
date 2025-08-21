using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BARQ.Application.Services
{
    public class TenantStateService : ITenantStateService
    {
        private readonly BarqDbContext _context;
        private readonly ILogger<TenantStateService> _logger;

        public TenantStateService(BarqDbContext context, ILogger<TenantStateService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async System.Threading.Tasks.Task<PagedResult<TenantStateDto>> GetTenantStatesAsync(ListRequest request)
        {
            try
            {
                var query = _context.TenantStates
                    .Include(ts => ts.Tenant)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(ts => ts.Tenant.Name.Contains(request.SearchTerm) ||
                                            ts.Status.Contains(request.SearchTerm) ||
                                            (ts.StatusReason != null && ts.StatusReason.Contains(request.SearchTerm)));
                }

                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    query = request.SortDirection?.ToLower() == "desc"
                        ? query.OrderByDescending(ts => EF.Property<object>(ts, request.SortBy))
                        : query.OrderBy(ts => EF.Property<object>(ts, request.SortBy));
                }
                else
                {
                    query = query.OrderByDescending(ts => ts.RequiresAttention)
                                 .ThenByDescending(ts => ts.LastActivityAt);
                }

                var totalCount = await query.CountAsync();
                var tenantStates = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var tenantStateDtos = tenantStates.Select(MapToDto).ToList();

                return new PagedResult<TenantStateDto>
                {
                    Items = tenantStateDtos,
                    Total = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenant states");
                throw;
            }
        }

        public async System.Threading.Tasks.Task<TenantStateDto?> GetTenantStateByIdAsync(Guid id)
        {
            try
            {
                var tenantState = await _context.TenantStates
                    .Include(ts => ts.Tenant)
                    .FirstOrDefaultAsync(ts => ts.Id == id);

                return tenantState != null ? MapToDto(tenantState) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenant state by ID: {Id}", id);
                throw;
            }
        }

        public async System.Threading.Tasks.Task<TenantStateDto?> GetTenantStateByTenantIdAsync(Guid tenantId)
        {
            try
            {
                var tenantState = await _context.TenantStates
                    .Include(ts => ts.Tenant)
                    .FirstOrDefaultAsync(ts => ts.TenantId == tenantId);

                return tenantState != null ? MapToDto(tenantState) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenant state by tenant ID: {TenantId}", tenantId);
                throw;
            }
        }

        public async System.Threading.Tasks.Task<TenantStateDto?> UpdateTenantStateAsync(Guid tenantId, UpdateTenantStateRequest request, string updatedBy)
        {
            try
            {
                var tenantState = await _context.TenantStates
                    .Include(ts => ts.Tenant)
                    .FirstOrDefaultAsync(ts => ts.TenantId == tenantId);

                if (tenantState == null)
                {
                    return null;
                }

                var previousStatus = tenantState.Status;
                tenantState.Status = request.Status;
                tenantState.StatusReason = request.StatusReason;
                tenantState.StatusChangedAt = DateTime.UtcNow;
                tenantState.StatusChangedBy = updatedBy;
                tenantState.UpdatedAt = DateTime.UtcNow;
                tenantState.UpdatedBy = null;

                await _context.SaveChangesAsync();

                await LogTenantStateHistoryAsync(tenantState.Id, tenantId, previousStatus, request.Status, request.Reason, updatedBy);

                _logger.LogInformation("Tenant state updated: {TenantId} from {PreviousStatus} to {NewStatus} by {UpdatedBy}",
                    tenantId, previousStatus, request.Status, updatedBy);

                return MapToDto(tenantState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tenant state: {TenantId}", tenantId);
                throw;
            }
        }

        public async System.Threading.Tasks.Task RefreshTenantStateAsync(Guid tenantId)
        {
            try
            {
                var tenantState = await _context.TenantStates
                    .FirstOrDefaultAsync(ts => ts.TenantId == tenantId);

                if (tenantState == null)
                {
                    var tenant = await _context.Tenants.FindAsync(tenantId);
                    if (tenant == null)
                    {
                        return;
                    }

                    tenantState = new TenantState
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        Status = "Active",
                        IsHealthy = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = null
                    };

                    _context.TenantStates.Add(tenantState);
                }

                await UpdateTenantUsageStatsInternalAsync(tenantState);
                tenantState.LastHealthCheck = DateTime.UtcNow;
                tenantState.UpdatedAt = DateTime.UtcNow;
                tenantState.UpdatedBy = null;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Tenant state refreshed: {TenantId}", tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing tenant state: {TenantId}", tenantId);
                throw;
            }
        }

        public async System.Threading.Tasks.Task RefreshAllTenantStatesAsync()
        {
            try
            {
                var tenants = await _context.Tenants.ToListAsync();
                
                foreach (var tenant in tenants)
                {
                    await RefreshTenantStateAsync(tenant.Id);
                }

                _logger.LogInformation("All tenant states refreshed for {Count} tenants", tenants.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing all tenant states");
                throw;
            }
        }

        public async System.Threading.Tasks.Task<List<TenantStateDto>> GetTenantsRequiringAttentionAsync()
        {
            try
            {
                var tenantStates = await _context.TenantStates
                    .Include(ts => ts.Tenant)
                    .Where(ts => ts.RequiresAttention)
                    .OrderByDescending(ts => ts.UpdatedAt)
                    .ToListAsync();

                return tenantStates.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenants requiring attention");
                throw;
            }
        }

        public async System.Threading.Tasks.Task<List<TenantStateDto>> GetUnhealthyTenantsAsync()
        {
            try
            {
                var tenantStates = await _context.TenantStates
                    .Include(ts => ts.Tenant)
                    .Where(ts => !ts.IsHealthy)
                    .OrderByDescending(ts => ts.UpdatedAt)
                    .ToListAsync();

                return tenantStates.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unhealthy tenants");
                throw;
            }
        }

        public async System.Threading.Tasks.Task<Dictionary<string, object>> GetTenantStatsSummaryAsync()
        {
            try
            {
                var totalTenants = await _context.TenantStates.CountAsync();
                var healthyTenants = await _context.TenantStates.CountAsync(ts => ts.IsHealthy);
                var tenantsRequiringAttention = await _context.TenantStates.CountAsync(ts => ts.RequiresAttention);
                var activeTenants = await _context.TenantStates.CountAsync(ts => ts.Status == "Active");
                var suspendedTenants = await _context.TenantStates.CountAsync(ts => ts.Status == "Suspended");

                var totalUsers = await _context.TenantStates.SumAsync(ts => ts.CurrentUserCount);
                var totalProjects = await _context.TenantStates.SumAsync(ts => ts.CurrentProjectCount);
                var totalTasks = await _context.TenantStates.SumAsync(ts => ts.CurrentTaskCount);
                var totalStorageBytes = await _context.TenantStates.SumAsync(ts => ts.StorageUsedBytes);

                return new Dictionary<string, object>
                {
                    ["TotalTenants"] = totalTenants,
                    ["HealthyTenants"] = healthyTenants,
                    ["TenantsRequiringAttention"] = tenantsRequiringAttention,
                    ["ActiveTenants"] = activeTenants,
                    ["SuspendedTenants"] = suspendedTenants,
                    ["TotalUsers"] = totalUsers,
                    ["TotalProjects"] = totalProjects,
                    ["TotalTasks"] = totalTasks,
                    ["TotalStorageBytes"] = totalStorageBytes,
                    ["TotalStorageGB"] = Math.Round(totalStorageBytes / (1024.0 * 1024.0 * 1024.0), 2)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenant stats summary");
                throw;
            }
        }

        public async System.Threading.Tasks.Task UpdateTenantUsageStatsAsync(Guid tenantId)
        {
            try
            {
                var tenantState = await _context.TenantStates
                    .FirstOrDefaultAsync(ts => ts.TenantId == tenantId);

                if (tenantState != null)
                {
                    await UpdateTenantUsageStatsInternalAsync(tenantState);
                    tenantState.UpdatedAt = DateTime.UtcNow;
                    tenantState.UpdatedBy = null;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tenant usage stats: {TenantId}", tenantId);
                throw;
            }
        }

        public async System.Threading.Tasks.Task MarkTenantForAttentionAsync(Guid tenantId, string reason, string markedBy)
        {
            try
            {
                var tenantState = await _context.TenantStates
                    .FirstOrDefaultAsync(ts => ts.TenantId == tenantId);

                if (tenantState != null)
                {
                    tenantState.RequiresAttention = true;
                    tenantState.AttentionReason = reason;
                    tenantState.UpdatedAt = DateTime.UtcNow;
                    tenantState.UpdatedBy = null;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Tenant marked for attention: {TenantId} - {Reason} by {MarkedBy}",
                        tenantId, reason, markedBy);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking tenant for attention: {TenantId}", tenantId);
                throw;
            }
        }

        public async System.Threading.Tasks.Task ClearTenantAttentionAsync(Guid tenantId, string clearedBy)
        {
            try
            {
                var tenantState = await _context.TenantStates
                    .FirstOrDefaultAsync(ts => ts.TenantId == tenantId);

                if (tenantState != null)
                {
                    tenantState.RequiresAttention = false;
                    tenantState.AttentionReason = null;
                    tenantState.UpdatedAt = DateTime.UtcNow;
                    tenantState.UpdatedBy = null;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Tenant attention cleared: {TenantId} by {ClearedBy}",
                        tenantId, clearedBy);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing tenant attention: {TenantId}", tenantId);
                throw;
            }
        }

        private async System.Threading.Tasks.Task UpdateTenantUsageStatsInternalAsync(TenantState tenantState)
        {
            var tenantId = tenantState.TenantId;

            tenantState.CurrentUserCount = await _context.Users
                .CountAsync(u => u.TenantId == tenantId && !u.IsDeleted);

            tenantState.CurrentProjectCount = await _context.Projects
                .CountAsync(p => p.TenantId == tenantId && !p.IsDeleted);

            tenantState.CurrentTaskCount = await _context.Tasks
                .CountAsync(t => t.TenantId == tenantId && !t.IsDeleted);

            var lastActivity = await _context.AuditLogs
                .Where(al => al.TenantId == tenantId)
                .OrderByDescending(al => al.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastActivity != null)
            {
                tenantState.LastActivityAt = lastActivity.CreatedAt;
                tenantState.LastActivityBy = lastActivity.UserId.ToString();
            }

            var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
            tenantState.APICallsThisMonth = await _context.UsageRecords
                .Where(ur => ur.TenantId == tenantId && 
                           ur.UsageType == "APICall" && 
                           ur.BillingPeriod == currentMonth)
                .SumAsync(ur => ur.Quantity);

            tenantState.WorkflowExecutionsThisMonth = await _context.UsageRecords
                .Where(ur => ur.TenantId == tenantId && 
                           ur.UsageType == "WorkflowExecution" && 
                           ur.BillingPeriod == currentMonth)
                .SumAsync(ur => ur.Quantity);

            tenantState.StorageUsedBytes = await _context.FileAttachments
                .Where(fa => fa.TenantId == tenantId && !fa.IsDeleted)
                .SumAsync(fa => fa.FileSize);

            var subscription = await _context.TenantSubscriptions
                .Where(ts => ts.TenantId == tenantId && ts.Status == "Active")
                .FirstOrDefaultAsync();

            if (subscription != null)
            {
                tenantState.BillingStatus = subscription.Status switch
                {
                    "Active" => "Current",
                    "PastDue" => "Overdue",
                    "Suspended" => "Suspended",
                    _ => "Unknown"
                };
            }

            tenantState.DatabaseStatus = "Healthy";
            tenantState.StorageStatus = "Healthy";
            tenantState.IsHealthy = tenantState.DatabaseStatus == "Healthy" && 
                                  tenantState.StorageStatus == "Healthy" &&
                                  tenantState.BillingStatus != "Suspended";
        }

        private async System.Threading.Tasks.Task LogTenantStateHistoryAsync(Guid tenantStateId, Guid tenantId, string previousStatus, string newStatus, string reason, string changedBy)
        {
            try
            {
                var history = new TenantStateHistory
                {
                    Id = Guid.NewGuid(),
                    TenantStateId = tenantStateId,
                    TenantId = tenantId,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    Reason = reason,
                    ChangedBy = changedBy,
                    ChangedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = null
                };

                _context.TenantStateHistory.Add(history);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging tenant state history for tenant: {TenantId}", tenantId);
            }
        }

        private static TenantStateDto MapToDto(TenantState tenantState)
        {
            var storageUsedGB = tenantState.StorageUsedBytes / (1024.0 * 1024.0 * 1024.0);
            var daysSinceLastActivity = tenantState.LastActivityAt.HasValue 
                ? (DateTime.UtcNow - tenantState.LastActivityAt.Value).Days 
                : 0;

            return new TenantStateDto
            {
                Id = tenantState.Id.ToString(),
                TenantId = tenantState.TenantId.ToString(),
                TenantName = tenantState.Tenant?.Name ?? "Unknown",
                Status = tenantState.Status,
                StatusReason = tenantState.StatusReason,
                StatusChangedAt = tenantState.StatusChangedAt,
                StatusChangedBy = tenantState.StatusChangedBy,
                IsHealthy = tenantState.IsHealthy,
                LastHealthCheck = tenantState.LastHealthCheck,
                CurrentUserCount = tenantState.CurrentUserCount,
                CurrentProjectCount = tenantState.CurrentProjectCount,
                CurrentTaskCount = tenantState.CurrentTaskCount,
                StorageUsedBytes = tenantState.StorageUsedBytes,
                APICallsThisMonth = tenantState.APICallsThisMonth,
                WorkflowExecutionsThisMonth = tenantState.WorkflowExecutionsThisMonth,
                LastActivityAt = tenantState.LastActivityAt,
                LastActivityBy = tenantState.LastActivityBy,
                DatabaseStatus = tenantState.DatabaseStatus,
                StorageStatus = tenantState.StorageStatus,
                BillingStatus = tenantState.BillingStatus,
                RequiresAttention = tenantState.RequiresAttention,
                AttentionReason = tenantState.AttentionReason,
                StorageUsedFormatted = $"{storageUsedGB:F2} GB",
                StorageUsagePercent = 0, // Would need quota info to calculate
                IsOverQuota = false, // Would need quota info to calculate
                DaysSinceLastActivity = daysSinceLastActivity
            };
        }
    }
}
