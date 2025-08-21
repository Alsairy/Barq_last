using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;

namespace BARQ.Application.Services
{
    public sealed class TenantStateService : ITenantStateService
    {
        public System.Threading.Tasks.Task<PagedResult<TenantStateDto>> GetTenantStatesAsync(ListRequest request)
        {
            return System.Threading.Tasks.Task.FromResult(new PagedResult<TenantStateDto>());
        }

        public System.Threading.Tasks.Task<TenantStateDto?> GetTenantStateByIdAsync(Guid id)
        {
            return System.Threading.Tasks.Task.FromResult<TenantStateDto?>(null);
        }

        public System.Threading.Tasks.Task<TenantStateDto?> GetTenantStateByTenantIdAsync(Guid tenantId)
        {
            return System.Threading.Tasks.Task.FromResult<TenantStateDto?>(null);
        }

        public System.Threading.Tasks.Task<TenantStateDto?> UpdateTenantStateAsync(Guid tenantId, UpdateTenantStateRequest request, string updatedBy)
        {
            return System.Threading.Tasks.Task.FromResult<TenantStateDto?>(null);
        }

        public System.Threading.Tasks.Task RefreshTenantStateAsync(Guid tenantId)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task RefreshAllTenantStatesAsync()
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task<List<TenantStateDto>> GetTenantsRequiringAttentionAsync()
        {
            return System.Threading.Tasks.Task.FromResult(new List<TenantStateDto>());
        }

        public System.Threading.Tasks.Task<List<TenantStateDto>> GetUnhealthyTenantsAsync()
        {
            return System.Threading.Tasks.Task.FromResult(new List<TenantStateDto>());
        }

        public System.Threading.Tasks.Task<Dictionary<string, object>> GetTenantStatsSummaryAsync()
        {
            return System.Threading.Tasks.Task.FromResult(new Dictionary<string, object>());
        }

        public System.Threading.Tasks.Task UpdateTenantUsageStatsAsync(Guid tenantId)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task MarkTenantForAttentionAsync(Guid tenantId, string reason, string markedBy)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task ClearTenantAttentionAsync(Guid tenantId, string clearedBy)
        {
<<<<<<< HEAD
            try
            {
                var tenantState = await _context.TenantStates
                    .FirstOrDefaultAsync(ts => ts.TenantId == tenantId);

                if (tenantState != null)
                {
                    tenantState.RequiresAttention = true;
                    tenantState.AttentionReason = reason;
                    tenantState.UpdatedAt = DateTime.UtcNow;
                    tenantState.UpdatedBy = Guid.TryParse(markedBy, out var markedByGuid) ? markedByGuid : null;

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

        public async Task ClearTenantAttentionAsync(Guid tenantId, string clearedBy)
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
                    tenantState.UpdatedBy = Guid.TryParse(clearedBy, out var clearedByGuid) ? clearedByGuid : null;

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

        private async Task UpdateTenantUsageStatsInternalAsync(TenantState tenantState)
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
                tenantState.LastActivityBy = lastActivity.UserId?.ToString();
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

        private async Task LogTenantStateHistoryAsync(Guid tenantStateId, Guid tenantId, string previousStatus, string newStatus, string reason, string changedBy)
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
                    CreatedBy = Guid.TryParse(changedBy, out var changedByGuid) ? changedByGuid : null
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
||||||| f8d500a
            try
            {
                var tenantState = await _context.TenantStates
                    .FirstOrDefaultAsync(ts => ts.TenantId == tenantId);

                if (tenantState != null)
                {
                    tenantState.RequiresAttention = true;
                    tenantState.AttentionReason = reason;
                    tenantState.UpdatedAt = DateTime.UtcNow;
                    tenantState.UpdatedBy = markedBy;

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

        public async Task ClearTenantAttentionAsync(Guid tenantId, string clearedBy)
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
                    tenantState.UpdatedBy = clearedBy;

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

        private async Task UpdateTenantUsageStatsInternalAsync(TenantState tenantState)
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
                tenantState.LastActivityBy = lastActivity.UserId;
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
                .SumAsync(fa => fa.FileSizeBytes);

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

        private async Task LogTenantStateHistoryAsync(Guid tenantStateId, Guid tenantId, string previousStatus, string newStatus, string reason, string changedBy)
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
                    CreatedBy = changedBy
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
=======
            return System.Threading.Tasks.Task.CompletedTask;
>>>>>>> origin/main
        }
    }
}
