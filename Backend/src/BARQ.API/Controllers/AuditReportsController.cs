using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Models.Responses;
using System.Security.Claims;

namespace BARQ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuditReportsController : ControllerBase
    {
        private readonly IAuditReportService _auditReportService;
        private readonly ILogger<AuditReportsController> _logger;

        public AuditReportsController(IAuditReportService auditReportService, ILogger<AuditReportsController> logger)
        {
            _auditReportService = auditReportService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<AuditReportDto>>> CreateReport([FromBody] CreateAuditReportRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                var report = await _auditReportService.CreateReportAsync(userId, tenantId, request);

                return Ok(new ApiResponse<AuditReportDto>
                {
                    Success = true,
                    Data = report,
                    Message = "Audit report created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating audit report");
                return StatusCode(500, new ApiResponse<AuditReportDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the audit report"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<AuditReportDto>>> GetReport(string id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                var report = await _auditReportService.GetReportAsync(id, userId, tenantId);

                if (report == null)
                {
                    return NotFound(new ApiResponse<AuditReportDto>
                    {
                        Success = false,
                        Message = "Audit report not found"
                    });
                }

                return Ok(new ApiResponse<AuditReportDto>
                {
                    Success = true,
                    Data = report
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit report {ReportId}", id);
                return StatusCode(500, new ApiResponse<AuditReportDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the audit report"
                });
            }
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<AuditReportDto>>>> GetReports([FromQuery] AuditReportListRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                var reports = await _auditReportService.GetReportsAsync(userId, tenantId, request);

                return Ok(new ApiResponse<PagedResult<AuditReportDto>>
                {
                    Success = true,
                    Data = reports
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit reports");
                return StatusCode(500, new ApiResponse<PagedResult<AuditReportDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving audit reports"
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteReport(string id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                var result = await _auditReportService.DeleteReportAsync(id, userId, tenantId);

                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Audit report not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Audit report deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting audit report {ReportId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while deleting the audit report"
                });
            }
        }

        [HttpPost("{id}/generate")]
        public async Task<ActionResult<ApiResponse<string>>> GenerateReport(string id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                var result = await _auditReportService.GenerateReportAsync(id, userId, tenantId);

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Data = result,
                    Message = "Report generation initiated"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating audit report {ReportId}", id);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "An error occurred while generating the audit report"
                });
            }
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadReport(string id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                var stream = await _auditReportService.DownloadReportAsync(id, userId, tenantId);

                if (stream == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Report file not found or not ready for download"
                    });
                }

                return File(stream, "application/octet-stream", $"audit_report_{id}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading audit report {ReportId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while downloading the audit report"
                });
            }
        }

        [HttpGet("audit-logs")]
        public async Task<ActionResult<ApiResponse<PagedResult<AuditLogViewDto>>>> GetAuditLogs([FromQuery] AuditLogSearchRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                var auditLogs = await _auditReportService.GetAuditLogsAsync(userId, tenantId, request);

                return Ok(new ApiResponse<PagedResult<AuditLogViewDto>>
                {
                    Success = true,
                    Data = auditLogs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs");
                return StatusCode(500, new ApiResponse<PagedResult<AuditLogViewDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving audit logs"
                });
            }
        }

        [HttpPost("audit-logs/export")]
        public async Task<IActionResult> ExportAuditLogs([FromBody] AuditLogExportRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                var stream = await _auditReportService.ExportAuditLogsAsync(userId, tenantId, request);

                var contentType = request.Format.ToUpper() switch
                {
                    "CSV" => "text/csv",
                    "EXCEL" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "PDF" => "application/pdf",
                    _ => "application/octet-stream"
                };

                var fileName = $"audit_logs_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{request.Format.ToLower()}";

                return File(stream, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting audit logs");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while exporting audit logs"
                });
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        private Guid? GetCurrentTenantId()
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;
            return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : null;
        }
    }
}
