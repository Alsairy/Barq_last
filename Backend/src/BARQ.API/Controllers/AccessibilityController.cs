using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BARQ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccessibilityController : ControllerBase
    {
        private readonly IAccessibilityService _accessibilityService;
        private readonly ILogger<AccessibilityController> _logger;

        public AccessibilityController(IAccessibilityService accessibilityService, ILogger<AccessibilityController> logger)
        {
            _accessibilityService = accessibilityService;
            _logger = logger;
        }

        [HttpGet("audits")]
        public async Task<ActionResult<PagedResult<AccessibilityAuditDto>>> GetAccessibilityAudits([FromQuery] ListRequest request)
        {
            try
            {
                var result = await _accessibilityService.GetAccessibilityAuditsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accessibility audits");
                return StatusCode(500, "An error occurred while retrieving accessibility audits");
            }
        }

        [HttpGet("audits/{id}")]
        public async Task<ActionResult<AccessibilityAuditDto>> GetAccessibilityAudit(Guid id)
        {
            try
            {
                var audit = await _accessibilityService.GetAccessibilityAuditByIdAsync(id);
                if (audit == null)
                {
                    return NotFound();
                }

                return Ok(audit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accessibility audit: {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the accessibility audit");
            }
        }

        [HttpGet("audits/page")]
        public async Task<ActionResult<List<AccessibilityAuditDto>>> GetAccessibilityAuditsByPage([FromQuery] string pageUrl)
        {
            try
            {
                var audits = await _accessibilityService.GetAccessibilityAuditsByPageAsync(pageUrl);
                return Ok(audits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accessibility audits by page: {PageUrl}", pageUrl);
                return StatusCode(500, "An error occurred while retrieving accessibility audits");
            }
        }

        [HttpPost("audits")]
        [Authorize(Roles = "Admin,SuperAdmin,AccessibilityAuditor")]
        public async Task<ActionResult<AccessibilityAuditDto>> CreateAccessibilityAudit([FromBody] CreateAccessibilityAuditRequest request)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var audit = await _accessibilityService.CreateAccessibilityAuditAsync(request, userId);
                return CreatedAtAction(nameof(GetAccessibilityAudit), new { id = audit.Id }, audit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating accessibility audit");
                return StatusCode(500, "An error occurred while creating the accessibility audit");
            }
        }

        [HttpPut("audits/{id}")]
        [Authorize(Roles = "Admin,SuperAdmin,AccessibilityAuditor")]
        public async Task<ActionResult<AccessibilityAuditDto>> UpdateAccessibilityAudit(Guid id, [FromBody] CreateAccessibilityAuditRequest request)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var audit = await _accessibilityService.UpdateAccessibilityAuditAsync(id, request, userId);
                if (audit == null)
                {
                    return NotFound();
                }

                return Ok(audit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating accessibility audit: {Id}", id);
                return StatusCode(500, "An error occurred while updating the accessibility audit");
            }
        }

        [HttpDelete("audits/{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> DeleteAccessibilityAudit(Guid id)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var success = await _accessibilityService.DeleteAccessibilityAuditAsync(id, userId);
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting accessibility audit: {Id}", id);
                return StatusCode(500, "An error occurred while deleting the accessibility audit");
            }
        }

        [HttpPost("issues")]
        [Authorize(Roles = "Admin,SuperAdmin,AccessibilityAuditor")]
        public async Task<ActionResult<AccessibilityIssueDto>> CreateAccessibilityIssue([FromBody] CreateAccessibilityIssueRequest request)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var issue = await _accessibilityService.CreateAccessibilityIssueAsync(request, userId);
                return CreatedAtAction(nameof(GetAccessibilityAudit), new { id = request.AccessibilityAuditId }, issue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating accessibility issue");
                return StatusCode(500, "An error occurred while creating the accessibility issue");
            }
        }

        [HttpPut("issues/{id}")]
        [Authorize(Roles = "Admin,SuperAdmin,AccessibilityAuditor")]
        public async Task<ActionResult<AccessibilityIssueDto>> UpdateAccessibilityIssue(Guid id, [FromBody] CreateAccessibilityIssueRequest request)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var issue = await _accessibilityService.UpdateAccessibilityIssueAsync(id, request, userId);
                if (issue == null)
                {
                    return NotFound();
                }

                return Ok(issue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating accessibility issue: {Id}", id);
                return StatusCode(500, "An error occurred while updating the accessibility issue");
            }
        }

        [HttpDelete("issues/{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> DeleteAccessibilityIssue(Guid id)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var success = await _accessibilityService.DeleteAccessibilityIssueAsync(id, userId);
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting accessibility issue: {Id}", id);
                return StatusCode(500, "An error occurred while deleting the accessibility issue");
            }
        }

        [HttpGet("audits/{auditId}/issues")]
        public async Task<ActionResult<PagedResult<AccessibilityIssueDto>>> GetAccessibilityIssues(Guid auditId, [FromQuery] ListRequest request)
        {
            try
            {
                var result = await _accessibilityService.GetAccessibilityIssuesAsync(auditId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accessibility issues for audit: {AuditId}", auditId);
                return StatusCode(500, "An error occurred while retrieving accessibility issues");
            }
        }

        [HttpGet("issues/severity/{severity}")]
        public async Task<ActionResult<List<AccessibilityIssueDto>>> GetAccessibilityIssuesBySeverity(string severity)
        {
            try
            {
                var issues = await _accessibilityService.GetAccessibilityIssuesBySeverityAsync(severity);
                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accessibility issues by severity: {Severity}", severity);
                return StatusCode(500, "An error occurred while retrieving accessibility issues");
            }
        }

        [HttpGet("issues/status/{status}")]
        public async Task<ActionResult<List<AccessibilityIssueDto>>> GetAccessibilityIssuesByStatus(string status)
        {
            try
            {
                var issues = await _accessibilityService.GetAccessibilityIssuesByStatusAsync(status);
                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accessibility issues by status: {Status}", status);
                return StatusCode(500, "An error occurred while retrieving accessibility issues");
            }
        }

        [HttpPost("issues/{id}/assign")]
        [Authorize(Roles = "Admin,SuperAdmin,AccessibilityAuditor")]
        public async Task<ActionResult> AssignAccessibilityIssue(Guid id, [FromBody] AssignIssueRequest request)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var success = await _accessibilityService.AssignAccessibilityIssueAsync(id, request.AssignedTo, userId);
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning accessibility issue: {Id}", id);
                return StatusCode(500, "An error occurred while assigning the accessibility issue");
            }
        }

        [HttpPost("issues/{id}/resolve")]
        [Authorize(Roles = "Admin,SuperAdmin,Developer")]
        public async Task<ActionResult> ResolveAccessibilityIssue(Guid id, [FromBody] ResolveIssueRequest request)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var success = await _accessibilityService.ResolveAccessibilityIssueAsync(id, request.FixNotes, userId);
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving accessibility issue: {Id}", id);
                return StatusCode(500, "An error occurred while resolving the accessibility issue");
            }
        }

        [HttpPost("issues/{id}/verify")]
        [Authorize(Roles = "Admin,SuperAdmin,AccessibilityAuditor")]
        public async Task<ActionResult> VerifyAccessibilityIssue(Guid id, [FromBody] VerifyIssueRequest request)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var success = await _accessibilityService.VerifyAccessibilityIssueAsync(id, request.TestingNotes, userId);
                if (!success)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying accessibility issue: {Id}", id);
                return StatusCode(500, "An error occurred while verifying the accessibility issue");
            }
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<Dictionary<string, object>>> GetAccessibilityStats()
        {
            try
            {
                var stats = await _accessibilityService.GetAccessibilityStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accessibility stats");
                return StatusCode(500, "An error occurred while retrieving accessibility statistics");
            }
        }

        [HttpGet("wcag-compliance")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<Dictionary<string, object>>> GetWCAGComplianceReport([FromQuery] string? pageUrl = null)
        {
            try
            {
                var report = await _accessibilityService.GetWCAGComplianceReportAsync(pageUrl);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting WCAG compliance report");
                return StatusCode(500, "An error occurred while retrieving the WCAG compliance report");
            }
        }

        [HttpGet("issues/critical")]
        public async Task<ActionResult<List<AccessibilityIssueDto>>> GetCriticalAccessibilityIssues()
        {
            try
            {
                var issues = await _accessibilityService.GetCriticalAccessibilityIssuesAsync();
                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting critical accessibility issues");
                return StatusCode(500, "An error occurred while retrieving critical accessibility issues");
            }
        }

        [HttpPost("issues/bulk-update")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> BulkUpdateAccessibilityIssues([FromBody] BulkUpdateIssuesRequest request)
        {
            try
            {
                var userId = User.Identity?.Name ?? "Unknown";
                var success = await _accessibilityService.BulkUpdateAccessibilityIssuesAsync(request.IssueIds, request.Status, userId);
                if (!success)
                {
                    return BadRequest("Failed to update issues");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk updating accessibility issues");
                return StatusCode(500, "An error occurred while updating accessibility issues");
            }
        }
    }

    public class AssignIssueRequest
    {
        public string AssignedTo { get; set; } = string.Empty;
    }

    public class ResolveIssueRequest
    {
        public string FixNotes { get; set; } = string.Empty;
    }

    public class VerifyIssueRequest
    {
        public string TestingNotes { get; set; } = string.Empty;
    }

    public class BulkUpdateIssuesRequest
    {
        public List<Guid> IssueIds { get; set; } = new();
        public string Status { get; set; } = string.Empty;
    }
}
