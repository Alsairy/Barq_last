using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;
using BARQ.Infrastructure.Data;
using System.Text.Json;

namespace BARQ.Application.Services
{
    public class AuditReportService : IAuditReportService
    {
        private readonly BarqDbContext _context;
        private readonly ILogger<AuditReportService> _logger;

        public AuditReportService(BarqDbContext context, ILogger<AuditReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async System.Threading.Tasks.Task<AuditReportDto> CreateReportAsync(Guid userId, Guid? tenantId, CreateAuditReportRequest request)
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

        public async System.Threading.Tasks.Task<AuditReportDto?> GetReportAsync(string reportId, Guid userId, Guid? tenantId)
        {
            var report = await _context.AuditReports
                .Include(r => r.GeneratedByUser)
                .Where(r => r.Id == Guid.Parse(reportId))
                .Where(r => tenantId == null || r.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (report == null)
            {
                return null;
            }

            return await MapToDto(report);
        }

        public async System.Threading.Tasks.Task<PagedResult<AuditReportDto>> GetReportsAsync(Guid userId, Guid? tenantId, AuditReportListRequest request)
        {
            var query = _context.AuditReports
                .Include(r => r.GeneratedByUser)
                .Where(r => tenantId == null || r.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Status))
            {
                query = query.Where(r => r.Status == request.Status);
            }

            if (!string.IsNullOrEmpty(request.Format))
            {
                query = query.Where(r => r.Format == request.Format);
            }

            if (request.GeneratedAfter.HasValue)
            {
                query = query.Where(r => r.GeneratedAt >= request.GeneratedAfter.Value);
            }

            if (request.GeneratedBefore.HasValue)
            {
                query = query.Where(r => r.GeneratedAt <= request.GeneratedBefore.Value);
            }

            if (request.IsScheduled.HasValue)
            {
                query = query.Where(r => r.IsScheduled == request.IsScheduled.Value);
            }

            if (!string.IsNullOrEmpty(request.GeneratedBy))
            {
                query = query.Where(r => r.GeneratedBy == Guid.Parse(request.GeneratedBy));
            }

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
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async System.Threading.Tasks.Task<bool> DeleteReportAsync(string reportId, Guid userId, Guid? tenantId)
        {
            var report = await _context.AuditReports
                .Where(r => r.Id == Guid.Parse(reportId))
                .Where(r => tenantId == null || r.TenantId == tenantId)
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

        public async System.Threading.Tasks.Task<string> GenerateReportAsync(string reportId, Guid userId, Guid? tenantId)
        {
            var report = await _context.AuditReports
                .Where(r => r.Id == Guid.Parse(reportId))
                .Where(r => tenantId == null || r.TenantId == tenantId)
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

        public async System.Threading.Tasks.Task<Stream?> DownloadReportAsync(string reportId, Guid userId, Guid? tenantId)
        {
            var report = await _context.AuditReports
                .Where(r => r.Id == Guid.Parse(reportId))
                .Where(r => tenantId == null || r.TenantId == tenantId)
                .Where(r => r.Status == "Completed")
                .FirstOrDefaultAsync();

            if (report == null || string.IsNullOrEmpty(report.FilePath) || !File.Exists(report.FilePath))
            {
                return null;
            }

            return new FileStream(report.FilePath, FileMode.Open, FileAccess.Read);
        }

        public async System.Threading.Tasks.Task<PagedResult<AuditLogViewDto>> GetAuditLogsAsync(Guid userId, Guid? tenantId, AuditLogSearchRequest request)
        {
            var query = _context.AuditLogs
                .Include(a => a.User)
                .Include(a => a.Tenant)
                .Where(a => tenantId == null || a.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.EntityType))
            {
                query = query.Where(a => a.EntityType == request.EntityType);
            }

            if (!string.IsNullOrEmpty(request.EntityId))
            {
                query = query.Where(a => a.EntityId.ToString() == request.EntityId);
            }

            if (!string.IsNullOrEmpty(request.Action))
            {
                query = query.Where(a => a.Action == request.Action);
            }

            if (!string.IsNullOrEmpty(request.UserId))
            {
                query = query.Where(a => a.UserId == Guid.Parse(request.UserId));
            }

            if (request.StartDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= request.EndDate.Value);
            }

            if (!string.IsNullOrEmpty(request.IpAddress))
            {
                query = query.Where(a => a.IpAddress == request.IpAddress);
            }

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
                UserId = a.UserId?.ToString() ?? string.Empty,
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
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async System.Threading.Tasks.Task<Stream> ExportAuditLogsAsync(Guid userId, Guid? tenantId, AuditLogExportRequest request)
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
                .Where(r => r.IsScheduled && r.NextRunAt <= DateTime.UtcNow)
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
                .Where(r => r.ExpiresAt <= DateTime.UtcNow)
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
                var report = await _context.AuditReports.FindAsync(Guid.Parse(reportId));
                if (report == null) return;

                report.Status = "Generating";
                await _context.SaveChangesAsync();

                await System.Threading.Tasks.Task.Delay(5000); // 5 second delay

                var fileName = $"audit_report_{reportId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{report.Format.ToLower()}";
                var filePath = Path.Combine(Path.GetTempPath(), "barq_reports", fileName);
                
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                await File.WriteAllTextAsync(filePath, $"Mock audit report content for {report.Name}");

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
                
                var report = await _context.AuditReports.FindAsync(Guid.Parse(reportId));
                if (report != null)
                {
                    report.Status = "Failed";
                    report.ErrorMessage = ex.Message;
                    await _context.SaveChangesAsync();
                }
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
