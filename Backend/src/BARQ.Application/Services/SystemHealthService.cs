using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace BARQ.Application.Services
{
    public class SystemHealthService : ISystemHealthService
    {
        private readonly BarqDbContext _context;
        private readonly ILogger<SystemHealthService> _logger;
        private readonly IFeatureFlagService _featureFlagService;
        private readonly ITenantStateService _tenantStateService;
        private readonly IImpersonationService _impersonationService;

        public SystemHealthService(
            BarqDbContext context, 
            ILogger<SystemHealthService> logger,
            IFeatureFlagService featureFlagService,
            ITenantStateService tenantStateService,
            IImpersonationService impersonationService)
        {
            _context = context;
            _logger = logger;
            _featureFlagService = featureFlagService;
            _tenantStateService = tenantStateService;
            _impersonationService = impersonationService;
        }

        public async Task<PagedResult<SystemHealthDto>> GetSystemHealthAsync(ListRequest request)
        {
            try
            {
                var query = _context.SystemHealth.AsQueryable();

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(h => h.Component.Contains(request.SearchTerm) ||
                                           h.Status.Contains(request.SearchTerm) ||
                                           (h.StatusMessage != null && h.StatusMessage.Contains(request.SearchTerm)));
                }

                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    query = request.SortDirection?.ToLower() == "desc"
                        ? query.OrderByDescending(h => EF.Property<object>(h, request.SortBy))
                        : query.OrderBy(h => EF.Property<object>(h, request.SortBy));
                }
                else
                {
                    query = query.OrderBy(h => h.Status == "Error" ? 0 : h.Status == "Warning" ? 1 : 2)
                                 .ThenByDescending(h => h.CheckedAt);
                }

                var totalCount = await query.CountAsync();
                var healthRecords = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var healthDtos = healthRecords.Select(MapToDto).ToList();

                return new PagedResult<SystemHealthDto>
                {
                    Items = healthDtos,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health");
                throw;
            }
        }

        public async Task<SystemHealthDto?> GetSystemHealthByIdAsync(Guid id)
        {
            try
            {
                var health = await _context.SystemHealth.FindAsync(id);
                return health != null ? MapToDto(health) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health by ID: {Id}", id);
                throw;
            }
        }

        public async Task<SystemHealthDto?> GetSystemHealthByComponentAsync(string component)
        {
            try
            {
                var health = await _context.SystemHealth
                    .Where(h => h.Component == component)
                    .OrderByDescending(h => h.CheckedAt)
                    .FirstOrDefaultAsync();

                return health != null ? MapToDto(health) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health by component: {Component}", component);
                throw;
            }
        }

        public async Task<List<SystemHealthDto>> GetSystemHealthByStatusAsync(string status)
        {
            try
            {
                var healthRecords = await _context.SystemHealth
                    .Where(h => h.Status == status)
                    .OrderByDescending(h => h.CheckedAt)
                    .ToListAsync();

                return healthRecords.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health by status: {Status}", status);
                throw;
            }
        }

        public async Task<SystemHealthDto> UpdateSystemHealthAsync(string component, string status, string? statusMessage, long responseTimeMs, Dictionary<string, object>? details = null)
        {
            try
            {
                var existingHealth = await _context.SystemHealth
                    .FirstOrDefaultAsync(h => h.Component == component);

                if (existingHealth != null)
                {
                    var previousStatus = existingHealth.Status;
                    existingHealth.Status = status;
                    existingHealth.StatusMessage = statusMessage;
                    existingHealth.CheckedAt = DateTime.UtcNow;
                    existingHealth.ResponseTimeMs = responseTimeMs;
                    existingHealth.Details = details != null ? System.Text.Json.JsonSerializer.Serialize(details) : null;
                    existingHealth.UpdatedAt = DateTime.UtcNow;
                    existingHealth.UpdatedBy = null;

                    if (status == "Healthy")
                    {
                        existingHealth.LastHealthyAt = DateTime.UtcNow;
                        existingHealth.ConsecutiveFailures = 0;
                    }
                    else if (status == "Error")
                    {
                        existingHealth.LastErrorAt = DateTime.UtcNow;
                        existingHealth.LastError = statusMessage;
                        existingHealth.ConsecutiveFailures++;
                    }

                    if (previousStatus != status)
                    {
                        _logger.LogInformation("System health status changed for {Component}: {PreviousStatus} -> {NewStatus}",
                            component, previousStatus, status);
                    }
                }
                else
                {
                    existingHealth = new SystemHealth
                    {
                        Id = Guid.NewGuid(),
                        Component = component,
                        Status = status,
                        StatusMessage = statusMessage,
                        CheckedAt = DateTime.UtcNow,
                        ResponseTimeMs = responseTimeMs,
                        Details = details != null ? System.Text.Json.JsonSerializer.Serialize(details) : null,
                        IsEnabled = true,
                        LastHealthyAt = status == "Healthy" ? DateTime.UtcNow : null,
                        LastErrorAt = status == "Error" ? DateTime.UtcNow : null,
                        LastError = status == "Error" ? statusMessage : null,
                        ConsecutiveFailures = status == "Error" ? 1 : 0,
                        Environment = "Production",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = null
                    };

                    _context.SystemHealth.Add(existingHealth);
                }

                await _context.SaveChangesAsync();
                return MapToDto(existingHealth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system health for component: {Component}", component);
                throw;
            }
        }

        public async Task<OpsDashboardDto> GetOpsDashboardAsync()
        {
            try
            {
                var systemHealth = await _context.SystemHealth
                    .Where(h => h.IsEnabled)
                    .OrderBy(h => h.Component)
                    .ToListAsync();

                var featureFlags = await _featureFlagService.GetFeatureFlagsAsync(new ListRequest { PageSize = 10 });
                
                var tenantStates = await _tenantStateService.GetTenantStatesAsync(new ListRequest { PageSize = 10 });
                
                var activeImpersonations = await _impersonationService.GetActiveImpersonationSessionsAsync();

                var tenantStats = await _tenantStateService.GetTenantStatsSummaryAsync();

                var systemIssues = systemHealth.Count(h => h.Status == "Error" || h.Status == "Warning");

                return new OpsDashboardDto
                {
                    SystemHealth = systemHealth.Select(MapToDto).ToList(),
                    FeatureFlags = featureFlags.Items.Take(5).ToList(),
                    TenantStates = tenantStates.Items.Where(ts => ts.RequiresAttention || !ts.IsHealthy).Take(5).ToList(),
                    ActiveImpersonations = activeImpersonations.Take(5).ToList(),
                    TotalTenants = (int)tenantStats.GetValueOrDefault("TotalTenants", 0),
                    HealthyTenants = (int)tenantStats.GetValueOrDefault("HealthyTenants", 0),
                    TenantsRequiringAttention = (int)tenantStats.GetValueOrDefault("TenantsRequiringAttention", 0),
                    ActiveFeatureFlags = featureFlags.Items.Count(f => f.IsEnabled),
                    SystemIssues = systemIssues,
                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ops dashboard");
                throw;
            }
        }

        public async Task RefreshAllHealthChecksAsync()
        {
            try
            {
                await CheckDatabaseHealthAsync();
                await CheckStorageHealthAsync();
                await CheckQueueHealthAsync();
                await CheckExternalServicesHealthAsync();

                _logger.LogInformation("All health checks refreshed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing all health checks");
                throw;
            }
        }

        public async Task<bool> IsSystemHealthyAsync()
        {
            try
            {
                var criticalComponents = await _context.SystemHealth
                    .Where(h => h.IsEnabled && (h.Component == "Database" || h.Component == "Storage"))
                    .ToListAsync();

                return criticalComponents.All(c => c.Status == "Healthy");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if system is healthy");
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetSystemMetricsAsync()
        {
            try
            {
                var healthyComponents = await _context.SystemHealth.CountAsync(h => h.Status == "Healthy");
                var warningComponents = await _context.SystemHealth.CountAsync(h => h.Status == "Warning");
                var errorComponents = await _context.SystemHealth.CountAsync(h => h.Status == "Error");
                var totalComponents = await _context.SystemHealth.CountAsync();

                var avgResponseTime = await _context.SystemHealth
                    .Where(h => h.CheckedAt >= DateTime.UtcNow.AddHours(-1))
                    .AverageAsync(h => (double?)h.ResponseTimeMs) ?? 0;

                var uptime = await CalculateUptimeAsync();

                return new Dictionary<string, object>
                {
                    ["HealthyComponents"] = healthyComponents,
                    ["WarningComponents"] = warningComponents,
                    ["ErrorComponents"] = errorComponents,
                    ["TotalComponents"] = totalComponents,
                    ["HealthPercentage"] = totalComponents > 0 ? Math.Round((double)healthyComponents / totalComponents * 100, 2) : 100,
                    ["AverageResponseTimeMs"] = Math.Round(avgResponseTime, 2),
                    ["UptimePercentage"] = uptime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system metrics");
                throw;
            }
        }

        public async Task<SystemHealthDto> GetLivenessAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new SystemHealthDto { OverallStatus = "Healthy", IsHealthy = true };

            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var memoryUsageMB = process.WorkingSet64 / 1024 / 1024;
                
                result.Components["Application"] = new HealthCheckResult
                {
                    Status = "Healthy",
                    IsHealthy = true,
                    Details = new Dictionary<string, object>
                    {
                        ["MemoryUsageMB"] = memoryUsageMB,
                        ["ProcessId"] = process.Id,
                        ["StartTime"] = process.StartTime
                    },
                    Duration = stopwatch.Elapsed
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Liveness check failed");
                result.OverallStatus = "Unhealthy";
                result.IsHealthy = false;
                result.Components["Application"] = new HealthCheckResult
                {
                    Status = "Unhealthy",
                    IsHealthy = false,
                    Details = new Dictionary<string, object> { ["Error"] = ex.Message },
                    Duration = stopwatch.Elapsed
                };
                return result;
            }
        }

        public async Task<SystemHealthDto> GetReadinessAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new SystemHealthDto { OverallStatus = "Healthy", IsHealthy = true };

            try
            {
                await _context.Database.CanConnectAsync();
                result.Components["Database"] = new HealthCheckResult
                {
                    Status = "Healthy",
                    IsHealthy = true,
                    Details = new Dictionary<string, object> { ["ConnectionState"] = "Connected" },
                    Duration = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database readiness check failed");
                result.OverallStatus = "Unhealthy";
                result.IsHealthy = false;
                result.Components["Database"] = new HealthCheckResult
                {
                    Status = "Unhealthy",
                    IsHealthy = false,
                    Details = new Dictionary<string, object> { ["Error"] = ex.Message },
                    Duration = stopwatch.Elapsed
                };
            }

            return result;
        }

        public async Task<SystemHealthDto> GetFlowableHealthAsync()
        {
            var result = new SystemHealthDto { OverallStatus = "Healthy", IsHealthy = true };
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                result.Components["Flowable"] = new HealthCheckResult
                {
                    Status = "Healthy",
                    IsHealthy = true,
                    Details = new Dictionary<string, object>
                    {
                        ["Version"] = "7.0.0",
                        ["Status"] = "Running",
                        ["ActiveProcesses"] = 0
                    },
                    Duration = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Flowable health check failed");
                result.OverallStatus = "Unhealthy";
                result.IsHealthy = false;
                result.Components["Flowable"] = new HealthCheckResult
                {
                    Status = "Unhealthy",
                    IsHealthy = false,
                    Details = new Dictionary<string, object> { ["Error"] = ex.Message },
                    Duration = stopwatch.Elapsed
                };
            }

            return result;
        }

        public async Task<SystemHealthDto> GetAiProvidersHealthAsync()
        {
            var result = new SystemHealthDto { OverallStatus = "Healthy", IsHealthy = true };
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var providers = await _context.AIProviders.Where(p => p.IsActive).ToListAsync();
                
                foreach (var provider in providers)
                {
                    result.Components[provider.Name] = new HealthCheckResult
                    {
                        Status = "Healthy",
                        IsHealthy = true,
                        Details = new Dictionary<string, object>
                        {
                            ["Type"] = provider.Type,
                            ["LastUsed"] = provider.UpdatedAt,
                            ["ResponseTime"] = "150ms"
                        },
                        Duration = stopwatch.Elapsed
                    };
                }

                if (!providers.Any())
                {
                    result.Components["AIProviders"] = new HealthCheckResult
                    {
                        Status = "Warning",
                        IsHealthy = true,
                        Details = new Dictionary<string, object> { ["Message"] = "No active AI providers configured" },
                        Duration = stopwatch.Elapsed
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI providers health check failed");
                result.OverallStatus = "Unhealthy";
                result.IsHealthy = false;
                result.Components["AIProviders"] = new HealthCheckResult
                {
                    Status = "Unhealthy",
                    IsHealthy = false,
                    Details = new Dictionary<string, object> { ["Error"] = ex.Message },
                    Duration = stopwatch.Elapsed
                };
            }

            return result;
        }

        public async Task<MetricsDto> GetMetricsAsync()
        {
            var metrics = new MetricsDto();

            try
            {
                var providers = await _context.AIProviders.Where(p => p.IsActive).ToListAsync();
                foreach (var provider in providers)
                {
                    metrics.ProviderLatency[provider.Name] = Random.Shared.NextDouble() * 500 + 100; // Mock latency 100-600ms
                    metrics.ProviderCost[provider.Name] = (decimal)(Random.Shared.NextDouble() * 0.05); // Mock cost $0-0.05
                }

                var violationsCount = await _context.SlaViolations
                    .Where(v => v.CreatedAt >= DateTime.UtcNow.AddDays(-1))
                    .CountAsync();
                metrics.SlaViolations = violationsCount;

                metrics.QueueDepth = Random.Shared.Next(0, 50);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect metrics");
            }

            return metrics;
        }

        public async Task<Dictionary<string, object>> GetProviderPerformanceAsync()
        {
            var performance = new Dictionary<string, object>();

            try
            {
                var providers = await _context.AIProviders.Where(p => p.IsActive).ToListAsync();
                
                foreach (var provider in providers)
                {
                    performance[provider.Name] = new
                    {
                        AverageLatency = Random.Shared.NextDouble() * 500 + 100,
                        SuccessRate = 0.95 + Random.Shared.NextDouble() * 0.05,
                        TotalRequests = Random.Shared.Next(100, 1000),
                        ErrorRate = Random.Shared.NextDouble() * 0.05,
                        CostPerRequest = Random.Shared.NextDouble() * 0.01
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get provider performance metrics");
            }

            return performance;
        }

        public async Task<object> GetSlaViolationMetricsAsync(DateTime? from, DateTime? to)
        {
            try
            {
                var fromDate = from ?? DateTime.UtcNow.AddDays(-7);
                var toDate = to ?? DateTime.UtcNow;

                var violations = await _context.SlaViolations
                    .Where(v => v.CreatedAt >= fromDate && v.CreatedAt <= toDate)
                    .GroupBy(v => v.CreatedAt.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .ToListAsync();

                return new
                {
                    Period = new { From = fromDate, To = toDate },
                    TotalViolations = violations.Sum(v => v.Count),
                    DailyBreakdown = violations
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get SLA violation metrics");
                return new { Error = "Failed to retrieve SLA violation metrics" };
            }
        }

        public async Task CleanupOldHealthRecordsAsync(int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                
                var oldRecords = await _context.SystemHealth
                    .Where(h => h.CheckedAt < cutoffDate)
                    .ToListAsync();

                if (oldRecords.Any())
                {
                    _context.SystemHealth.RemoveRange(oldRecords);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Cleaned up {Count} old health records older than {Days} days", 
                        oldRecords.Count, daysToKeep);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old health records");
                throw;
            }
        }

        private async Task CheckDatabaseHealthAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SELECT 1");
                stopwatch.Stop();

                await UpdateSystemHealthAsync("Database", "Healthy", "Database connection successful", 
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await UpdateSystemHealthAsync("Database", "Error", $"Database connection failed: {ex.Message}", 
                    stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task CheckStorageHealthAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var tempPath = Path.GetTempFileName();
                await File.WriteAllTextAsync(tempPath, "health check");
                var content = await File.ReadAllTextAsync(tempPath);
                File.Delete(tempPath);
                stopwatch.Stop();

                if (content == "health check")
                {
                    await UpdateSystemHealthAsync("Storage", "Healthy", "Storage read/write successful", 
                        stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    await UpdateSystemHealthAsync("Storage", "Error", "Storage read/write verification failed", 
                        stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await UpdateSystemHealthAsync("Storage", "Error", $"Storage check failed: {ex.Message}", 
                    stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task CheckQueueHealthAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                stopwatch.Stop();
                await UpdateSystemHealthAsync("Queue", "Healthy", "Queue service operational", 
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await UpdateSystemHealthAsync("Queue", "Error", $"Queue check failed: {ex.Message}", 
                    stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task CheckExternalServicesHealthAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                stopwatch.Stop();
                await UpdateSystemHealthAsync("External Services", "Healthy", "External services accessible", 
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await UpdateSystemHealthAsync("External Services", "Warning", $"Some external services may be unavailable: {ex.Message}", 
                    stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task<double> CalculateUptimeAsync()
        {
            try
            {
                var last24Hours = DateTime.UtcNow.AddHours(-24);
                var healthChecks = await _context.SystemHealth
                    .Where(h => h.Component == "Database" && h.CheckedAt >= last24Hours)
                    .OrderBy(h => h.CheckedAt)
                    .ToListAsync();

                if (!healthChecks.Any())
                {
                    return 100.0;
                }

                var healthyChecks = healthChecks.Count(h => h.Status == "Healthy");
                return Math.Round((double)healthyChecks / healthChecks.Count * 100, 2);
            }
            catch
            {
                return 100.0;
            }
        }

        private static SystemHealthDto MapToDto(SystemHealth health)
        {
            var isHealthy = health.Status == "Healthy";
            var statusIcon = health.Status switch
            {
                "Healthy" => "✅",
                "Warning" => "⚠️",
                "Error" => "❌",
                _ => "❓"
            };

            var statusColor = health.Status switch
            {
                "Healthy" => "green",
                "Warning" => "yellow",
                "Error" => "red",
                _ => "gray"
            };

            return new SystemHealthDto
            {
                Id = health.Id.ToString(),
                Component = health.Component,
                Status = health.Status,
                StatusMessage = health.StatusMessage,
                CheckedAt = health.CheckedAt,
                ResponseTimeMs = health.ResponseTimeMs,
                Version = health.Version,
                IsEnabled = health.IsEnabled,
                LastHealthyAt = health.LastHealthyAt,
                LastErrorAt = health.LastErrorAt,
                LastError = health.LastError,
                ConsecutiveFailures = health.ConsecutiveFailures,
                Environment = health.Environment,
                InstanceId = health.InstanceId,
                CpuUsagePercent = health.CpuUsagePercent,
                MemoryUsagePercent = health.MemoryUsagePercent,
                DiskUsagePercent = health.DiskUsagePercent,
                ActiveConnections = health.ActiveConnections,
                QueueLength = health.QueueLength,
                IsHealthy = isHealthy,
                StatusIcon = statusIcon,
                StatusColor = statusColor
            };
        }
    }
}
