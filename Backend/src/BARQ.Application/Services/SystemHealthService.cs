using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Services;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BARQ.Application.Services
{
    public sealed class SystemHealthService : ISystemHealthService
    {
        private readonly IHttpClientFactory _hcf;
        private readonly BarqDbContext _context;
        private readonly ILogger<SystemHealthService> _logger;
        private readonly ITenantProvider _tenantProvider;

        public SystemHealthService(
            IHttpClientFactory hcf,
            BarqDbContext context,
            ILogger<SystemHealthService> logger,
            ITenantProvider tenantProvider)
        {
            _hcf = hcf;
            _context = context;
            _logger = logger;
            _tenantProvider = tenantProvider;
        }

        public async System.Threading.Tasks.Task<PagedResult<SystemHealthDto>> GetSystemHealthAsync(ListRequest request)
        {
            var healthChecks = new List<SystemHealthDto>
            {
                new SystemHealthDto { Component = "Database", Status = await DatabaseAsync(CancellationToken.None) ? "Healthy" : "Unhealthy" },
                new SystemHealthDto { Component = "Flowable", Status = await FlowableAsync(CancellationToken.None) ? "Healthy" : "Unhealthy" },
                new SystemHealthDto { Component = "AI Providers", Status = await AiProvidersAsync(CancellationToken.None) ? "Healthy" : "Unhealthy" }
            };
            
            return new PagedResult<SystemHealthDto>
            {
                Items = healthChecks,
                TotalCount = healthChecks.Count,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async System.Threading.Tasks.Task<SystemHealthDto?> GetSystemHealthByIdAsync(Guid id)
        {
            try
            {
                var health = await _context.SystemHealths
                    .Where(h => h.Id == id && !h.IsDeleted && h.TenantId == _tenantProvider.GetTenantId())
                    .Select(h => new SystemHealthDto
                    {
                        Id = h.Id.ToString(),
                        Component = h.Component,
                        Status = h.Status,
                        StatusMessage = h.StatusMessage,
                        ResponseTimeMs = h.ResponseTimeMs,
                        CheckedAt = h.CheckedAt,
                        LastChecked = h.CheckedAt,
                        Version = h.Version,
                        IsEnabled = h.IsEnabled,
                        LastHealthyAt = h.LastHealthyAt,
                        LastErrorAt = h.LastErrorAt,
                        LastError = h.LastError,
                        ConsecutiveFailures = h.ConsecutiveFailures,
                        Environment = h.Environment,
                        InstanceId = h.InstanceId,
                        CpuUsagePercent = h.CpuUsagePercent,
                        MemoryUsagePercent = h.MemoryUsagePercent,
                        DiskUsagePercent = h.DiskUsagePercent,
                        ActiveConnections = h.ActiveConnections,
                        QueueLength = h.QueueLength,
                        IsHealthy = h.Status == "Healthy"
                    })
                    .FirstOrDefaultAsync();

                if (health != null)
                {
                    health.StatusIcon = health.IsHealthy ? "✅" : "❌";
                    health.StatusColor = health.IsHealthy ? "green" : "red";
                    health.OverallStatus = health.IsHealthy ? "Healthy" : "Unhealthy";
                    health.Timestamp = DateTime.UtcNow;
                }

                return health;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get system health by ID {Id}", id);
                return null;
            }
        }

        public async System.Threading.Tasks.Task<SystemHealthDto?> GetSystemHealthByComponentAsync(string component)
        {
            try
            {
                var health = await _context.SystemHealths
                    .Where(h => h.Component == component && !h.IsDeleted && h.TenantId == _tenantProvider.GetTenantId())
                    .OrderByDescending(h => h.CheckedAt)
                    .Select(h => new SystemHealthDto
                    {
                        Id = h.Id.ToString(),
                        Component = h.Component,
                        Status = h.Status,
                        StatusMessage = h.StatusMessage,
                        ResponseTimeMs = h.ResponseTimeMs,
                        CheckedAt = h.CheckedAt,
                        LastChecked = h.CheckedAt,
                        Version = h.Version,
                        IsEnabled = h.IsEnabled,
                        LastHealthyAt = h.LastHealthyAt,
                        LastErrorAt = h.LastErrorAt,
                        LastError = h.LastError,
                        ConsecutiveFailures = h.ConsecutiveFailures,
                        Environment = h.Environment,
                        InstanceId = h.InstanceId,
                        CpuUsagePercent = h.CpuUsagePercent,
                        MemoryUsagePercent = h.MemoryUsagePercent,
                        DiskUsagePercent = h.DiskUsagePercent,
                        ActiveConnections = h.ActiveConnections,
                        QueueLength = h.QueueLength,
                        IsHealthy = h.Status == "Healthy"
                    })
                    .FirstOrDefaultAsync();

                if (health != null)
                {
                    health.StatusIcon = health.IsHealthy ? "✅" : "❌";
                    health.StatusColor = health.IsHealthy ? "green" : "red";
                    health.OverallStatus = health.IsHealthy ? "Healthy" : "Unhealthy";
                    health.Timestamp = DateTime.UtcNow;
                }

                return health;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get system health by component {Component}", component);
                return null;
            }
        }

        public async System.Threading.Tasks.Task<List<SystemHealthDto>> GetSystemHealthByStatusAsync(string status)
        {
            try
            {
                var healthRecords = await _context.SystemHealths
                    .Where(h => h.Status == status && !h.IsDeleted && h.TenantId == _tenantProvider.GetTenantId())
                    .OrderByDescending(h => h.CheckedAt)
                    .Select(h => new SystemHealthDto
                    {
                        Id = h.Id.ToString(),
                        Component = h.Component,
                        Status = h.Status,
                        StatusMessage = h.StatusMessage,
                        ResponseTimeMs = h.ResponseTimeMs,
                        CheckedAt = h.CheckedAt,
                        LastChecked = h.CheckedAt,
                        Version = h.Version,
                        IsEnabled = h.IsEnabled,
                        LastHealthyAt = h.LastHealthyAt,
                        LastErrorAt = h.LastErrorAt,
                        LastError = h.LastError,
                        ConsecutiveFailures = h.ConsecutiveFailures,
                        Environment = h.Environment,
                        InstanceId = h.InstanceId,
                        CpuUsagePercent = h.CpuUsagePercent,
                        MemoryUsagePercent = h.MemoryUsagePercent,
                        DiskUsagePercent = h.DiskUsagePercent,
                        ActiveConnections = h.ActiveConnections,
                        QueueLength = h.QueueLength,
                        IsHealthy = h.Status == "Healthy"
                    })
                    .ToListAsync();

                foreach (var health in healthRecords)
                {
                    health.StatusIcon = health.IsHealthy ? "✅" : "❌";
                    health.StatusColor = health.IsHealthy ? "green" : "red";
                    health.OverallStatus = health.IsHealthy ? "Healthy" : "Unhealthy";
                    health.Timestamp = DateTime.UtcNow;
                }

                return healthRecords;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get system health by status {Status}", status);
                return new List<SystemHealthDto>();
            }
        }

        public async System.Threading.Tasks.Task<SystemHealthDto> UpdateSystemHealthAsync(string component, string status, string? statusMessage, long responseTimeMs, Dictionary<string, object>? details = null)
        {
            try
            {
                var existingHealth = await _context.SystemHealths
                    .Where(h => h.Component == component && !h.IsDeleted && h.TenantId == _tenantProvider.GetTenantId())
                    .FirstOrDefaultAsync();

                if (existingHealth != null)
                {
                    existingHealth.Status = status;
                    existingHealth.StatusMessage = statusMessage;
                    existingHealth.ResponseTimeMs = responseTimeMs;
                    existingHealth.CheckedAt = DateTime.UtcNow;
                    existingHealth.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    existingHealth = new BARQ.Core.Entities.SystemHealth
                    {
                        Id = Guid.NewGuid(),
                        TenantId = _tenantProvider.GetTenantId(),
                        Component = component,
                        Status = status,
                        StatusMessage = statusMessage,
                        ResponseTimeMs = responseTimeMs,
                        CheckedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.SystemHealths.Add(existingHealth);
                }

                await _context.SaveChangesAsync();

                return new SystemHealthDto
                {
                    Id = existingHealth.Id.ToString(),
                    Component = existingHealth.Component,
                    Status = existingHealth.Status,
                    StatusMessage = existingHealth.StatusMessage,
                    ResponseTimeMs = existingHealth.ResponseTimeMs,
                    CheckedAt = existingHealth.CheckedAt,
                    LastChecked = existingHealth.CheckedAt,
                    IsHealthy = existingHealth.Status == "Healthy",
                    StatusIcon = existingHealth.Status == "Healthy" ? "✅" : "❌",
                    StatusColor = existingHealth.Status == "Healthy" ? "green" : "red",
                    OverallStatus = existingHealth.Status == "Healthy" ? "Healthy" : "Unhealthy",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update system health for component {Component}", component);
                throw;
            }
        }

        public async System.Threading.Tasks.Task<OpsDashboardDto> GetOpsDashboardAsync()
        {
            try
            {
                var healthyCount = await _context.SystemHealths
                    .Where(h => h.Status == "Healthy" && !h.IsDeleted && h.TenantId == _tenantProvider.GetTenantId())
                    .CountAsync();

                var unhealthyCount = await _context.SystemHealths
                    .Where(h => h.Status != "Healthy" && !h.IsDeleted && h.TenantId == _tenantProvider.GetTenantId())
                    .CountAsync();

                var recentAlerts = await _context.SystemHealths
                    .Where(h => h.Status != "Healthy" && h.CheckedAt > DateTime.UtcNow.AddHours(-24) && !h.IsDeleted && h.TenantId == _tenantProvider.GetTenantId())
                    .CountAsync();

                var avgResponseTime = await _context.SystemHealths
                    .Where(h => !h.IsDeleted && h.TenantId == _tenantProvider.GetTenantId())
                    .Select(h => h.ResponseTimeMs)
                    .DefaultIfEmpty(0)
                    .AverageAsync();

                var systemHealth = await _context.SystemHealths
                    .Where(h => !h.IsDeleted && h.TenantId == _tenantProvider.GetTenantId())
                    .OrderByDescending(h => h.CheckedAt)
                    .Take(5)
                    .Select(h => new SystemHealthDto
                    {
                        Id = h.Id.ToString(),
                        Component = h.Component,
                        Status = h.Status,
                        StatusMessage = h.StatusMessage,
                        ResponseTimeMs = h.ResponseTimeMs,
                        CheckedAt = h.CheckedAt,
                        LastChecked = h.CheckedAt,
                        IsHealthy = h.Status == "Healthy"
                    })
                    .ToListAsync();

                var featureFlags = await _context.FeatureFlags
                    .Where(f => !f.IsDeleted && f.TenantId == _tenantProvider.GetTenantId())
                    .OrderByDescending(f => f.UpdatedAt)
                    .Take(5)
                    .Select(f => new FeatureFlagDto
                    {
                        Id = f.Id,
                        Name = f.Name,
                        DisplayName = f.DisplayName,
                        IsEnabled = f.IsEnabled,
                        Environment = f.Environment,
                        Category = f.Category
                    })
                    .ToListAsync();

                var tenantStates = await _context.TenantStates
                    .Where(t => !t.IsDeleted)
                    .OrderByDescending(t => t.UpdatedAt)
                    .Take(5)
                    .Select(t => new TenantStateDto
                    {
                        Id = t.Id.ToString(),
                        TenantId = t.TenantId.ToString(),
                        TenantName = t.TenantName,
                        Status = t.Status,
                        IsHealthy = t.IsHealthy
                    })
                    .ToListAsync();

                return new OpsDashboardDto
                {
                    SystemHealth = systemHealth,
                    FeatureFlags = featureFlags,
                    TenantStates = tenantStates,
                    ActiveImpersonations = await _context.ImpersonationSessions
                        .Where(i => i.EndedAt == null && i.TenantId == _tenantProvider.GetTenantId())
                        .OrderByDescending(i => i.StartedAt)
                        .Take(5)
                        .Select(i => new ImpersonationSessionDto
                        {
                            Id = i.Id,
                            AdminUserId = i.AdminUserId,
                            AdminUserName = i.AdminUserName,
                            TargetUserId = i.TargetUserId,
                            TargetUserName = i.TargetUserName,
                            TenantId = i.TenantId,
                            TenantName = i.TenantName,
                            StartedAt = i.StartedAt,
                            EndedAt = i.EndedAt,
                            Status = i.Status,
                            Reason = i.Reason
                        })
                        .ToListAsync(),
                    TotalTenants = await _context.Tenants.CountAsync(),
                    HealthyTenants = await _context.TenantStates.Where(t => t.IsHealthy).CountAsync(),
                    TenantsRequiringAttention = await _context.TenantStates.Where(t => t.RequiresAttention).CountAsync(),
                    ActiveFeatureFlags = await _context.FeatureFlags.Where(f => f.IsEnabled && f.TenantId == _tenantProvider.GetTenantId()).CountAsync(),
                    SystemIssues = unhealthyCount,
                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get ops dashboard data");
                return new OpsDashboardDto();
            }
        }

        public async System.Threading.Tasks.Task RefreshAllHealthChecksAsync()
        {
            try
            {
                var tenantId = _tenantProvider.GetTenantId();
                var healthChecks = await _context.SystemHealths
                    .Where(h => h.TenantId == tenantId && !h.IsDeleted)
                    .ToListAsync();

                foreach (var healthCheck in healthChecks)
                {
                    healthCheck.CheckedAt = DateTime.UtcNow;
                    healthCheck.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Refreshed {Count} health checks for tenant {TenantId}", healthChecks.Count, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh health checks");
            }
        }

        public async System.Threading.Tasks.Task<bool> IsSystemHealthyAsync()
        {
            try
            {
                var dbHealthy = await DatabaseAsync(CancellationToken.None);
                var flowableHealthy = await FlowableAsync(CancellationToken.None);
                var aiProvidersHealthy = await AiProvidersAsync(CancellationToken.None);
                
                return dbHealthy && flowableHealthy && aiProvidersHealthy;
            }
            catch
            {
                return false;
            }
        }

        public async System.Threading.Tasks.Task<bool> DatabaseAsync(CancellationToken ct)
        {
            return true;
        }

        public async System.Threading.Tasks.Task<bool> FlowableAsync(CancellationToken ct)
        {
            try
            {
                var http = _hcf.CreateClient("flowable");
                var res = await http.GetAsync("repository/deployments?size=1", ct);
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async System.Threading.Tasks.Task<bool> AiProvidersAsync(CancellationToken ct)
        {
            return true;
        }

        public async System.Threading.Tasks.Task<Dictionary<string, object>> GetSystemMetricsAsync()
        {
            try
            {
                var healthyComponents = await _context.SystemHealths
                    .Where(h => h.Status == "Healthy" && !h.IsDeleted && h.TenantId == _tenantProvider.GetTenantId())
                    .CountAsync();

                var unhealthyComponents = await _context.SystemHealths
                    .Where(h => h.Status != "Healthy" && !h.IsDeleted && h.TenantId == _tenantProvider.GetTenantId())
                    .CountAsync();

                var avgResponseTime = await _context.SystemHealths
                    .Where(h => !h.IsDeleted && h.TenantId == _tenantProvider.GetTenantId())
                    .Select(h => h.ResponseTimeMs)
                    .DefaultIfEmpty(0)
                    .AverageAsync();

                var lastCheckTime = await _context.SystemHealths
                    .Where(h => !h.IsDeleted && h.TenantId == _tenantProvider.GetTenantId())
                    .MaxAsync(h => (DateTime?)h.CheckedAt) ?? DateTime.MinValue;

                return new Dictionary<string, object>
                {
                    ["HealthyComponents"] = healthyComponents,
                    ["UnhealthyComponents"] = unhealthyComponents,
                    ["TotalComponents"] = healthyComponents + unhealthyComponents,
                    ["AverageResponseTimeMs"] = Math.Round(avgResponseTime, 2),
                    ["LastCheckTime"] = lastCheckTime,
                    ["SystemUptime"] = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime(),
                    ["TenantId"] = _tenantProvider.GetTenantId().ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get system metrics");
                return new Dictionary<string, object>();
            }
        }

        public async System.Threading.Tasks.Task CleanupOldHealthRecordsAsync(int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var tenantId = _tenantProvider.GetTenantId();
                
                var oldRecords = await _context.SystemHealths
                    .Where(h => h.TenantId == tenantId && h.CheckedAt < cutoffDate)
                    .ToListAsync();

                if (oldRecords.Any())
                {
                    _context.SystemHealths.RemoveRange(oldRecords);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cleaned up {Count} old health records older than {Days} days", oldRecords.Count, daysToKeep);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old health records");
            }
        }

        public async System.Threading.Tasks.Task<SystemHealthDto> GetLivenessAsync()
        {
            try
            {
                var isHealthy = await _context.Database.CanConnectAsync();
                return new SystemHealthDto 
                { 
                    Component = "Application", 
                    Status = isHealthy ? "Healthy" : "Unhealthy", 
                    LastChecked = DateTime.UtcNow,
                    IsHealthy = isHealthy,
                    StatusIcon = isHealthy ? "✅" : "❌",
                    StatusColor = isHealthy ? "green" : "red"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check application liveness");
                return new SystemHealthDto 
                { 
                    Component = "Application", 
                    Status = "Unhealthy", 
                    LastChecked = DateTime.UtcNow,
                    IsHealthy = false,
                    StatusIcon = "❌",
                    StatusColor = "red",
                    LastError = ex.Message
                };
            }
        }

        public async System.Threading.Tasks.Task<SystemHealthDto> GetReadinessAsync()
        {
            var isReady = await IsSystemHealthyAsync();
            return new SystemHealthDto 
            { 
                Component = "System", 
                Status = isReady ? "Ready" : "NotReady", 
                LastChecked = DateTime.UtcNow 
            };
        }

        public async System.Threading.Tasks.Task<SystemHealthDto> GetFlowableHealthAsync()
        {
            var isHealthy = await FlowableAsync(CancellationToken.None);
            return new SystemHealthDto 
            { 
                Component = "Flowable", 
                Status = isHealthy ? "Healthy" : "Unhealthy", 
                LastChecked = DateTime.UtcNow 
            };
        }

        public async System.Threading.Tasks.Task<SystemHealthDto> GetAiProvidersHealthAsync()
        {
            var isHealthy = await AiProvidersAsync(CancellationToken.None);
            return new SystemHealthDto 
            { 
                Component = "AI Providers", 
                Status = isHealthy ? "Healthy" : "Unhealthy", 
                LastChecked = DateTime.UtcNow 
            };
        }

        public async System.Threading.Tasks.Task<MetricsDto> GetMetricsAsync()
        {
            try
            {
                var tenantId = _tenantProvider.GetTenantId();
                
                var totalTasks = await _context.Tasks
                    .Where(t => t.TenantId == tenantId && !t.IsDeleted)
                    .CountAsync();
                
                var completedTasks = await _context.Tasks
                    .Where(t => t.TenantId == tenantId && !t.IsDeleted && t.Status == "Completed")
                    .CountAsync();
                
                var activeTasks = await _context.Tasks
                    .Where(t => t.TenantId == tenantId && !t.IsDeleted && t.Status != "Completed")
                    .CountAsync();
                
                var avgResponseTime = await _context.SystemHealths
                    .Where(h => h.TenantId == tenantId && !h.IsDeleted)
                    .Select(h => h.ResponseTimeMs)
                    .DefaultIfEmpty(0)
                    .AverageAsync();

                return new MetricsDto
                {
                    TotalTasks = totalTasks,
                    CompletedTasks = completedTasks,
                    ActiveTasks = activeTasks,
                    AverageResponseTimeMs = (long)Math.Round(avgResponseTime),
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get metrics");
                return new MetricsDto { Timestamp = DateTime.UtcNow };
            }
        }

        public async System.Threading.Tasks.Task<Dictionary<string, object>> GetProviderPerformanceAsync()
        {
            try
            {
                var tenantId = _tenantProvider.GetTenantId();
                
                var aiProviders = await _context.AIProviders
                    .Where(p => p.TenantId == tenantId && !p.IsDeleted)
                    .ToListAsync();

                var performance = new Dictionary<string, object>();
                
                foreach (var provider in aiProviders)
                {
                    var avgResponseTime = await _context.SystemHealths
                        .Where(h => h.Component == provider.Name && h.TenantId == tenantId && !h.IsDeleted)
                        .Select(h => h.ResponseTimeMs)
                        .DefaultIfEmpty(0)
                        .AverageAsync();

                    var successRate = await _context.SystemHealths
                        .Where(h => h.Component == provider.Name && h.TenantId == tenantId && !h.IsDeleted)
                        .CountAsync(h => h.Status == "Healthy") * 100.0 / 
                        Math.Max(1, await _context.SystemHealths
                            .Where(h => h.Component == provider.Name && h.TenantId == tenantId && !h.IsDeleted)
                            .CountAsync());

                    performance[provider.Name] = new
                    {
                        AverageResponseTimeMs = Math.Round(avgResponseTime, 2),
                        SuccessRate = Math.Round(successRate, 2),
                        IsEnabled = provider.IsEnabled,
                        LastChecked = await _context.SystemHealths
                            .Where(h => h.Component == provider.Name && h.TenantId == tenantId && !h.IsDeleted)
                            .MaxAsync(h => (DateTime?)h.CheckedAt) ?? DateTime.MinValue
                    };
                }

                return performance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get provider performance metrics");
                return new Dictionary<string, object>();
            }
        }

        public async System.Threading.Tasks.Task<object> GetSlaViolationMetricsAsync(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var tenantId = _tenantProvider.GetTenantId();
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                var violations = await _context.SlaViolations
                    .Where(v => v.TenantId == tenantId && 
                               v.ViolationTime >= start && 
                               v.ViolationTime <= end && 
                               !v.IsDeleted)
                    .ToListAsync();

                var totalViolations = violations.Count;
                var criticalViolations = violations.Count(v => v.Severity == "Critical");
                var highViolations = violations.Count(v => v.Severity == "High");
                var mediumViolations = violations.Count(v => v.Severity == "Medium");

                return new Dictionary<string, object>
                {
                    ["TotalViolations"] = totalViolations,
                    ["CriticalViolations"] = criticalViolations,
                    ["HighViolations"] = highViolations,
                    ["MediumViolations"] = mediumViolations,
                    ["StartDate"] = start,
                    ["EndDate"] = end,
                    ["TenantId"] = tenantId.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get SLA violation metrics");
                return new Dictionary<string, object>();
            }
        }
    }
}
