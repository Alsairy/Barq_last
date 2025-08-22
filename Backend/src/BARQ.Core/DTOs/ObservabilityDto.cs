namespace BARQ.Core.DTOs
{
    public class FeatureFlagDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsEnabled { get; set; }
        public string Environment { get; set; } = string.Empty;
        public string? Category { get; set; }
        public DateTime? EnabledAt { get; set; }
        public DateTime? DisabledAt { get; set; }
        public string? EnabledBy { get; set; }
        public string? DisabledBy { get; set; }
        public string? ImpactDescription { get; set; }
        public int RolloutPercentage { get; set; }
        public bool RequiresRestart { get; set; }
        public bool IsSystemFlag { get; set; }
        public int Priority { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateFeatureFlagRequest
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsEnabled { get; set; } = false;
        public string Environment { get; set; } = "Development";
        public string? Category { get; set; }
        public string? ImpactDescription { get; set; }
        public bool RequiresRestart { get; set; } = false;
        public int Priority { get; set; } = 0;
    }

    public class UpdateFeatureFlagRequest
    {
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public bool? IsEnabled { get; set; }
        public string? Environment { get; set; }
        public string? Category { get; set; }
        public string? ImpactDescription { get; set; }
        public int? RolloutPercentage { get; set; }
        public bool? RequiresRestart { get; set; }
        public int? Priority { get; set; }
        public string? Reason { get; set; }
    }

    public class TenantStateDto
    {
        public string Id { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? StatusReason { get; set; }
        public DateTime? StatusChangedAt { get; set; }
        public string? StatusChangedBy { get; set; }
        public bool IsHealthy { get; set; }
        public DateTime? LastHealthCheck { get; set; }
        public long CurrentUserCount { get; set; }
        public long CurrentProjectCount { get; set; }
        public long CurrentTaskCount { get; set; }
        public long StorageUsedBytes { get; set; }
        public long APICallsThisMonth { get; set; }
        public long WorkflowExecutionsThisMonth { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public string? LastActivityBy { get; set; }
        public string? DatabaseStatus { get; set; }
        public string? StorageStatus { get; set; }
        public string? BillingStatus { get; set; }
        public bool RequiresAttention { get; set; }
        public string? AttentionReason { get; set; }
        public string StorageUsedFormatted { get; set; } = string.Empty;
        public double StorageUsagePercent { get; set; }
        public bool IsOverQuota { get; set; }
        public int DaysSinceLastActivity { get; set; }
    }

    public class UpdateTenantStateRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? StatusReason { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class ImpersonationSessionDto
    {
        public Guid Id { get; set; }
        public Guid AdminUserId { get; set; }
        public string AdminUserName { get; set; } = string.Empty;
        public Guid TargetUserId { get; set; }
        public string TargetUserName { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string SessionToken { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? TicketNumber { get; set; }
        public string? Notes { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? EndedBy { get; set; }
        public string? EndReason { get; set; }
        public string? IpAddress { get; set; }
        public int ActionCount { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsExpired { get; set; }
        public int DurationMinutes { get; set; }
        public List<ImpersonationActionDto> RecentActions { get; set; } = new List<ImpersonationActionDto>();
    }

    public class CreateImpersonationSessionRequest
    {
        public Guid TargetUserId { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? TicketNumber { get; set; }
        public string? Notes { get; set; }
        public int DurationMinutes { get; set; } = 60;
        public int? DurationHours { get; set; }
    }

    public class EndImpersonationSessionRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class ImpersonationActionDto
    {
        public string Id { get; set; } = string.Empty;
        public string? SessionId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime PerformedAt { get; set; }
        public string? IpAddress { get; set; }
        public string? HttpMethod { get; set; }
        public string? RequestPath { get; set; }
        public int ResponseStatusCode { get; set; }
        public int StatusCode { get; set; }
        public long ResponseTimeMs { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? RiskLevel { get; set; }
        public bool RequiresApproval { get; set; }
        public bool IsApproved { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }

    public class SystemHealthDto
    {
        public string Id { get; set; } = string.Empty;
        public string Component { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? StatusMessage { get; set; }
        public DateTime CheckedAt { get; set; }
        public DateTime LastChecked { get; set; }
        public long ResponseTimeMs { get; set; }
        public string? Version { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime? LastHealthyAt { get; set; }
        public DateTime? LastErrorAt { get; set; }
        public string? LastError { get; set; }
        public int ConsecutiveFailures { get; set; }
        public string? Environment { get; set; }
        public string? InstanceId { get; set; }
        public double? CpuUsagePercent { get; set; }
        public double? MemoryUsagePercent { get; set; }
        public double? DiskUsagePercent { get; set; }
        public long? ActiveConnections { get; set; }
        public long? QueueLength { get; set; }
        public bool IsHealthy { get; set; }
        public string StatusIcon { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        
        public string OverallStatus { get; set; } = string.Empty;
        public Dictionary<string, HealthCheckResult> Components { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class HealthCheckResult
    {
        public string Status { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class MetricsDto
    {
        public Dictionary<string, double> ProviderLatency { get; set; } = new();
        public Dictionary<string, decimal> ProviderCost { get; set; } = new();
        public int SlaViolations { get; set; }
        public int QueueDepth { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class OpsDashboardDto
    {
        public List<SystemHealthDto> SystemHealth { get; set; } = new List<SystemHealthDto>();
        public List<FeatureFlagDto> FeatureFlags { get; set; } = new List<FeatureFlagDto>();
        public List<TenantStateDto> TenantStates { get; set; } = new List<TenantStateDto>();
        public List<ImpersonationSessionDto> ActiveImpersonations { get; set; } = new List<ImpersonationSessionDto>();
        public int TotalTenants { get; set; }
        public int HealthyTenants { get; set; }
        public int TenantsRequiringAttention { get; set; }
        public int ActiveFeatureFlags { get; set; }
        public int SystemIssues { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
