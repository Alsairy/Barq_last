namespace BARQ.Core.DTOs
{
    public class AuditReportDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        public string GeneratedByName { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public long? FileSizeBytes { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsScheduled { get; set; }
        public string? ScheduleCron { get; set; }
        public DateTime? NextRunAt { get; set; }
        public string? DownloadUrl { get; set; }
        public bool CanDownload { get; set; }
        public string? TenantId { get; set; }
    }

    public class CreateAuditReportRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Format { get; set; } = "PDF";
        public string? Filters { get; set; }
        public bool IsScheduled { get; set; } = false;
        public string? ScheduleCron { get; set; }
        public string? TemplateId { get; set; }
    }

    public class AuditReportListRequest : Common.ListRequest
    {
        public string? Status { get; set; }
        public string? Format { get; set; }
        public DateTime? GeneratedAfter { get; set; }
        public DateTime? GeneratedBefore { get; set; }
        public bool? IsScheduled { get; set; }
        public string? GeneratedBy { get; set; }
        public string? Search { get; set; }
    }

    public class AuditLogViewDto
    {
        public string Id { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? Changes { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? TenantId { get; set; }
        public string? TenantName { get; set; }
        public string? AdditionalData { get; set; }
    }

    public class AuditLogSearchRequest : Common.ListRequest
    {
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? Action { get; set; }
        public string? UserId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? IpAddress { get; set; }
        public new string? SearchTerm { get; set; }
    }

    public class AuditLogExportRequest
    {
        public string Format { get; set; } = "CSV"; // CSV, Excel, PDF
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? EntityType { get; set; }
        public string? Action { get; set; }
        public string? UserId { get; set; }
        public bool IncludeChanges { get; set; } = true;
        public bool IncludeUserDetails { get; set; } = true;
    }
}
