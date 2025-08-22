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
    public class AuditReportService : IAuditReportService
    {
        private readonly BarqDbContext _context;
        private readonly ILogger<AuditReportService> _logger;
        private readonly ITenantProvider _tenantProvider;

        public AuditReportService(BarqDbContext context, ILogger<AuditReportService> logger, ITenantProvider tenantProvider)
        {
            _context = context;
            _logger = logger;
            _tenantProvider = tenantProvider;
        }

        public async Task<AuditReportDto> CreateReportAsync(Guid userId, Guid? tenantId, CreateAuditReportRequest request)
        {
            _logger.LogInformation("Creating audit report: {Name} by user {UserId}", request.Name, userId);

            var reportId = Guid.NewGuid().ToString();
            var report = new AuditReport
            {
                Id = Guid.Parse(reportId),
                Name = request.Name,
                Description = request.Description,
                GeneratedBy = userId,
                TenantId = tenantId ?? Guid.Empty,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Format = request.Format,
                Filters = request.Filters,
                IsScheduled = request.IsScheduled,
                ScheduleCron = request.ScheduleCron,
                Status = "Pending",
                ExpiresAt = DateTime.UtcNow.AddDays(30) // Reports expire after 30 days
            };

            if (request.IsScheduled && !string.IsNullOrEmpty(request.ScheduleCron))
            {
                report.NextRunAt = CalculateNextRunTime(request.ScheduleCron);
            }

            _context.AuditReports.Add(report);
            await _context.SaveChangesAsync();

            _ = System.Threading.Tasks.Task.Run(async () => await GenerateReportInternalAsync(reportId));

            _logger.LogInformation("Audit report created: {ReportId}", reportId);

            return await MapToDto(report);
        }

        public async Task<AuditReportDto?> GetReportAsync(string reportId, Guid userId, Guid? tenantId)
        {
            var report = await _context.AuditReports
                .Include(r => r.GeneratedByUser)
                .Where(r => r.TenantId == _tenantProvider.GetTenantId() && r.Id == Guid.Parse(reportId))
                .FirstOrDefaultAsync();

            if (report == null)
            {
                throw new ArgumentException("Report not found");
            }

            return await MapToDto(report);
        }

        public async Task<PagedResult<AuditReportDto>> GetReportsAsync(Guid userId, Guid? tenantId, AuditReportListRequest request)
        {
            var query = _context.AuditReports
                .Include(r => r.GeneratedByUser)
                .Where(r => r.TenantId == _tenantProvider.GetTenantId() &&
                           (string.IsNullOrEmpty(request.Status) || r.Status == request.Status) &&
                           (string.IsNullOrEmpty(request.Format) || r.Format == request.Format) &&
                           (!request.GeneratedAfter.HasValue || r.GeneratedAt >= request.GeneratedAfter.Value) &&
                           (!request.GeneratedBefore.HasValue || r.GeneratedAt <= request.GeneratedBefore.Value) &&
                           (!request.IsScheduled.HasValue || r.IsScheduled == request.IsScheduled.Value) &&
                           (string.IsNullOrEmpty(request.GeneratedBy) || r.GeneratedBy == Guid.Parse(request.GeneratedBy)))
                .AsQueryable();</newstr>

            if (!string.IsNullOrEmpty(request.Search))
            {
                query = query.Where(r => r.Name.Contains(request.Search) || 
                                       (r.Description != null && r.Description.Contains(request.Search)));
            }

            var totalCount = await query.CountAsync();

            var reports = await query
                .OrderByDescending(r => r.GeneratedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var reportDtos = new List<AuditReportDto>();
            foreach (var report in reports)
            {
                reportDtos.Add(await MapToDto(report));
            }

            return new PagedResult<AuditReportDto>
            {
                Items = reportDtos,
                Total = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<bool> DeleteReportAsync(string reportId, Guid userId, Guid? tenantId)
        {
            var report = await _context.AuditReports
                .Where(r => r.TenantId == _tenantProvider.GetTenantId() && r.Id == Guid.Parse(reportId))
                .FirstOrDefaultAsync();

            if (report == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(report.FilePath) && File.Exists(report.FilePath))
            {
                try
                {
                    File.Delete(report.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete report file: {FilePath}", report.FilePath);
                }
            }

            _context.AuditReports.Remove(report);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Audit report deleted: {ReportId} by user {UserId}", reportId, userId);

            return true;
        }

        public async Task<string> GenerateReportAsync(string reportId, Guid userId, Guid? tenantId)
        {
            var report = await _context.AuditReports
                .Where(r => r.TenantId == _tenantProvider.GetTenantId() && r.Id == Guid.Parse(reportId))
                .FirstOrDefaultAsync();

            if (report == null)
            {
                throw new ArgumentException("Report not found");
            }

            if (report.Status == "Generating")
            {
                return "Report is already being generated";
            }

            if (report.Status == "Completed")
            {
                return "Report has already been generated";
            }

            _ = System.Threading.Tasks.Task.Run(async () => await GenerateReportInternalAsync(reportId));

            return "Report generation started";
        }

        public async Task<Stream?> DownloadReportAsync(string reportId, Guid userId, Guid? tenantId)
        {
            var report = await _context.AuditReports
                .Where(r => r.TenantId == _tenantProvider.GetTenantId() && r.Id == Guid.Parse(reportId) && r.Status == "Completed")
                .FirstOrDefaultAsync();

            if (report == null || string.IsNullOrEmpty(report.FilePath) || !File.Exists(report.FilePath))
            {
                throw new ArgumentException("Report file not found or not available for download");
            }

            return new FileStream(report.FilePath, FileMode.Open, FileAccess.Read);
        }

        public async Task<PagedResult<AuditLogViewDto>> GetAuditLogsAsync(Guid userId, Guid? tenantId, AuditLogSearchRequest request)
        {
            var query = _context.AuditLogs
                .Include(a => a.User)
                .Include(a => a.Tenant)
                .Where(a => a.TenantId == _tenantProvider.GetTenantId() &&
                           (string.IsNullOrEmpty(request.EntityType) || a.EntityType == request.EntityType) &&
                           (string.IsNullOrEmpty(request.EntityId) || a.EntityId.ToString() == request.EntityId) &&
                           (string.IsNullOrEmpty(request.Action) || a.Action == request.Action) &&
                           (string.IsNullOrEmpty(request.UserId) || a.UserId == Guid.Parse(request.UserId)) &&
                           (!request.StartDate.HasValue || a.Timestamp >= request.StartDate.Value) &&
                           (!request.EndDate.HasValue || a.Timestamp <= request.EndDate.Value) &&
                           (string.IsNullOrEmpty(request.IpAddress) || a.IpAddress == request.IpAddress))
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(a => a.EntityType.Contains(request.SearchTerm) ||
                                       a.Action.Contains(request.SearchTerm) ||
                                       (a.OldValue != null && a.OldValue.Contains(request.SearchTerm)) ||
                                       (a.NewValue != null && a.NewValue.Contains(request.SearchTerm)));
            }

            var totalCount = await query.CountAsync();

            var auditLogs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var auditLogDtos = auditLogs.Select(a => new AuditLogViewDto
            {
                Id = a.Id.ToString(),
                EntityType = a.EntityType,
                EntityId = a.EntityId.ToString(),
                Action = a.Action,
                OldValues = a.OldValue,
                NewValues = a.NewValue,
                Changes = GenerateChangesSummary(a.OldValue, a.NewValue),
                Timestamp = a.Timestamp,
                UserId = a.UserId?.ToString() ?? "Unknown",
                UserName = a.User?.UserName ?? "Unknown",
                UserEmail = a.User?.Email,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                TenantId = a.TenantId.ToString(),
                TenantName = a.Tenant?.Name,
                AdditionalData = a.AdditionalData
            }).ToList();

            return new PagedResult<AuditLogViewDto>
            {
                Items = auditLogDtos,
                Total = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<Stream> ExportAuditLogsAsync(Guid userId, Guid? tenantId, AuditLogExportRequest request)
        {
            var searchRequest = new AuditLogSearchRequest
            {
                EntityType = request.EntityType,
                Action = request.Action,
                UserId = request.UserId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Page = 1,
                PageSize = 10000 // Large page size for export
            };

            var auditLogs = await GetAuditLogsAsync(userId, tenantId, searchRequest);

            var stream = new MemoryStream();
            
            if (request.Format.ToUpper() == "CSV")
            {
                await GenerateCsvExport(stream, auditLogs.Items.ToList(), request);
            }
            else if (request.Format.ToUpper() == "EXCEL")
            {
                await GenerateExcelExport(stream, auditLogs.Items.ToList(), request);
            }
            else if (request.Format.ToUpper() == "PDF")
            {
                await GeneratePdfExport(stream, auditLogs.Items.ToList(), request);
            }

            stream.Position = 0;
            return stream;
        }

        public async System.Threading.Tasks.Task ProcessScheduledReportsAsync()
        {
            var scheduledReports = await _context.AuditReports
                .Where(r => r.TenantId == _tenantProvider.GetTenantId() && r.IsScheduled && r.NextRunAt <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var report in scheduledReports)
            {
                try
                {
                    var newReport = new AuditReport
                    {
                        Id = Guid.NewGuid(),
                        Name = $"{report.Name} - {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                        Description = report.Description,
                        GeneratedBy = report.GeneratedBy,
                        TenantId = report.TenantId,
                        StartDate = DateTime.UtcNow.AddDays(-7), // Last 7 days
                        EndDate = DateTime.UtcNow,
                        Format = report.Format,
                        Filters = report.Filters,
                        Status = "Pending",
                        ExpiresAt = DateTime.UtcNow.AddDays(30)
                    };

                    _context.AuditReports.Add(newReport);
                    
                    report.NextRunAt = CalculateNextRunTime(report.ScheduleCron!);
                    
                    await _context.SaveChangesAsync();

                    _ = System.Threading.Tasks.Task.Run(async () => await GenerateReportInternalAsync(newReport.Id.ToString()));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process scheduled report: {ReportId}", report.Id);
                }
            }
        }

        public async System.Threading.Tasks.Task CleanupExpiredReportsAsync()
        {
            var expiredReports = await _context.AuditReports
                .Where(r => r.TenantId == _tenantProvider.GetTenantId() && r.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var report in expiredReports)
            {
                if (!string.IsNullOrEmpty(report.FilePath) && File.Exists(report.FilePath))
                {
                    try
                    {
                        File.Delete(report.FilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete expired report file: {FilePath}", report.FilePath);
                    }
                }

                _context.AuditReports.Remove(report);
            }

            if (expiredReports.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} expired audit reports", expiredReports.Count);
            }
        }

        private async System.Threading.Tasks.Task GenerateReportInternalAsync(string reportId)
        {
            try
            {
                var report = await _context.AuditReports
                    .Where(r => r.TenantId == _tenantProvider.GetTenantId() && r.Id == Guid.Parse(reportId))
                    .FirstOrDefaultAsync();
                if (report == null) return;

                report.Status = "Generating";
                await _context.SaveChangesAsync();

                await System.Threading.Tasks.Task.Delay(5000); // 5 second delay

                var fileName = $"audit_report_{reportId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{report.Format.ToLower()}";
                var filePath = Path.Combine(Path.GetTempPath(), "barq_reports", fileName);
                
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                var auditData = await GenerateAuditReportDataAsync(report);
                await File.WriteAllTextAsync(filePath, auditData);

                report.Status = "Completed";
                report.FilePath = filePath;
                report.FileSizeBytes = new FileInfo(filePath).Length;
                report.CompletedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Audit report generated successfully: {ReportId}", reportId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate audit report: {ReportId}", reportId);
                
                var report = await _context.AuditReports
                    .Where(r => r.TenantId == _tenantProvider.GetTenantId() && r.Id == Guid.Parse(reportId))
                    .FirstOrDefaultAsync();
                if (report != null)
                {
                    report.Status = "Failed";
                    report.ErrorMessage = ex.Message;
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task<string> GenerateAuditReportDataAsync(AuditReport report)
        {
            var auditLogs = await _context.AuditLogs
                .Where(a => a.TenantId == report.TenantId && 
                           a.Timestamp >= report.StartDate && 
                           a.Timestamp <= report.EndDate)
                .Include(a => a.User)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            if (report.Format.ToUpper() == "JSON")
            {
                return JsonSerializer.Serialize(auditLogs.Select(a => new
                {
                    a.Id,
                    a.EntityType,
                    a.EntityId,
                    a.Action,
                    a.Timestamp,
                    UserName = a.User?.UserName,
                    a.IpAddress,
                    a.OldValue,
                    a.NewValue
                }), new JsonSerializerOptions { WriteIndented = true });
            }
            else if (report.Format.ToUpper() == "CSV")
            {
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Timestamp,EntityType,EntityId,Action,UserName,IpAddress,OldValue,NewValue");
                
                foreach (var log in auditLogs)
                {
                    csv.AppendLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss},{log.EntityType},{log.EntityId},{log.Action},{log.User?.UserName},{log.IpAddress},\"{log.OldValue}\",\"{log.NewValue}\"");
                }
                
                return csv.ToString();
            }
            else
            {
                var report_content = new System.Text.StringBuilder();
                report_content.AppendLine($"Audit Report: {report.Name}");
                report_content.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                report_content.AppendLine($"Period: {report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}");
                report_content.AppendLine($"Total Records: {auditLogs.Count}");
                report_content.AppendLine();
                
                foreach (var log in auditLogs)
                {
                    report_content.AppendLine($"[{log.Timestamp:yyyy-MM-dd HH:mm:ss}] {log.EntityType} {log.Action} by {log.User?.UserName} from {log.IpAddress}");
                }
                
                return report_content.ToString();
            }
        }

        private async Task<AuditReportDto> MapToDto(AuditReport report)
        {
            return new AuditReportDto
            {
                Id = report.Id.ToString(),
                Name = report.Name,
                Description = report.Description,
                GeneratedBy = report.GeneratedBy.ToString(),
                GeneratedByName = report.GeneratedByUser?.UserName ?? "Unknown",
                GeneratedAt = report.GeneratedAt,
                StartDate = report.StartDate,
                EndDate = report.EndDate,
                Status = report.Status,
                Format = report.Format,
                FileSizeBytes = report.FileSizeBytes,
                ErrorMessage = report.ErrorMessage,
                CompletedAt = report.CompletedAt,
                ExpiresAt = report.ExpiresAt,
                IsScheduled = report.IsScheduled,
                ScheduleCron = report.ScheduleCron,
                NextRunAt = report.NextRunAt,
                CanDownload = report.Status == "Completed" && !string.IsNullOrEmpty(report.FilePath),
                TenantId = report.TenantId.ToString()
            };
        }

        private string? GenerateChangesSummary(string? oldValues, string? newValues)
        {
            if (string.IsNullOrEmpty(oldValues) || string.IsNullOrEmpty(newValues))
                return null;

            try
            {
                var oldDict = JsonSerializer.Deserialize<Dictionary<string, object>>(oldValues);
                var newDict = JsonSerializer.Deserialize<Dictionary<string, object>>(newValues);
                
                var changes = new List<string>();
                
                if (oldDict != null && newDict != null)
                {
                    foreach (var key in newDict.Keys)
                    {
                        if (oldDict.ContainsKey(key) && !Equals(oldDict[key], newDict[key]))
                        {
                            changes.Add($"{key}: '{oldDict[key]}' → '{newDict[key]}'");
                        }
                    }
                }

                return changes.Any() ? string.Join(", ", changes) : null;
            }
            catch
            {
                return "Unable to parse changes";
            }
        }

        private DateTime? CalculateNextRunTime(string cronExpression)
        {
            return DateTime.UtcNow.AddHours(24); // Default to daily
        }

        private async System.Threading.Tasks.Task GenerateCsvExport(Stream stream, IList<AuditLogViewDto> auditLogs, AuditLogExportRequest request)
        {
            using var writer = new StreamWriter(stream, leaveOpen: true);
            
            await writer.WriteLineAsync("Timestamp,EntityType,EntityId,Action,UserId,UserName,UserEmail,IpAddress,Changes");
            
            foreach (var log in auditLogs)
            {
                var line = $"{log.Timestamp:yyyy-MM-dd HH:mm:ss},{log.EntityType},{log.EntityId},{log.Action},{log.UserId},{log.UserName},{log.UserEmail},{log.IpAddress},\"{log.Changes}\"";
                await writer.WriteLineAsync(line);
            }
        }

        private async System.Threading.Tasks.Task GenerateExcelExport(Stream stream, IList<AuditLogViewDto> auditLogs, AuditLogExportRequest request)
        {
            await GenerateCsvExport(stream, auditLogs, request);
        }

        private async System.Threading.Tasks.Task GeneratePdfExport(Stream stream, IList<AuditLogViewDto> auditLogs, AuditLogExportRequest request)
        {
            using var writer = new StreamWriter(stream, leaveOpen: true);
            await writer.WriteLineAsync("PDF Audit Log Export");
            await writer.WriteLineAsync($"Generated: {DateTime.UtcNow}");
            await writer.WriteLineAsync($"Total Records: {auditLogs.Count}");
        }
    }
}
