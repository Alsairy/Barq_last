using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;

namespace BARQ.Application.Interfaces
{
    public interface IAuditReportService
    {
        Task<AuditReportDto> CreateReportAsync(Guid userId, Guid? tenantId, CreateAuditReportRequest request);
        Task<AuditReportDto?> GetReportAsync(string reportId, Guid userId, Guid? tenantId);
        Task<PagedResult<AuditReportDto>> GetReportsAsync(Guid userId, Guid? tenantId, AuditReportListRequest request);
        Task<bool> DeleteReportAsync(string reportId, Guid userId, Guid? tenantId);
        Task<string> GenerateReportAsync(string reportId, Guid userId, Guid? tenantId);
        Task<Stream?> DownloadReportAsync(string reportId, Guid userId, Guid? tenantId);
        Task<PagedResult<AuditLogViewDto>> GetAuditLogsAsync(Guid userId, Guid? tenantId, AuditLogSearchRequest request);
        Task<Stream> ExportAuditLogsAsync(Guid userId, Guid? tenantId, AuditLogExportRequest request);
        System.Threading.Tasks.Task ProcessScheduledReportsAsync();
        System.Threading.Tasks.Task CleanupExpiredReportsAsync();
    }
}
