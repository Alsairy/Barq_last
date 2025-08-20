using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;

namespace BARQ.Application.Interfaces
{
    public interface IAccessibilityService
    {
        Task<PagedResult<AccessibilityAuditDto>> GetAccessibilityAuditsAsync(ListRequest request);
        Task<AccessibilityAuditDto?> GetAccessibilityAuditByIdAsync(Guid id);
        Task<List<AccessibilityAuditDto>> GetAccessibilityAuditsByPageAsync(string pageUrl);
        Task<AccessibilityAuditDto> CreateAccessibilityAuditAsync(CreateAccessibilityAuditRequest request, string createdBy);
        Task<AccessibilityAuditDto?> UpdateAccessibilityAuditAsync(Guid id, CreateAccessibilityAuditRequest request, string updatedBy);
        Task<bool> DeleteAccessibilityAuditAsync(Guid id, string deletedBy);
        Task<AccessibilityIssueDto> CreateAccessibilityIssueAsync(CreateAccessibilityIssueRequest request, string createdBy);
        Task<AccessibilityIssueDto?> UpdateAccessibilityIssueAsync(Guid id, CreateAccessibilityIssueRequest request, string updatedBy);
        Task<bool> DeleteAccessibilityIssueAsync(Guid id, string deletedBy);
        Task<PagedResult<AccessibilityIssueDto>> GetAccessibilityIssuesAsync(Guid auditId, ListRequest request);
        Task<List<AccessibilityIssueDto>> GetAccessibilityIssuesBySeverityAsync(string severity);
        Task<List<AccessibilityIssueDto>> GetAccessibilityIssuesByStatusAsync(string status);
        Task<bool> AssignAccessibilityIssueAsync(Guid id, string assignedTo, string assignedBy);
        Task<bool> ResolveAccessibilityIssueAsync(Guid id, string fixNotes, string resolvedBy);
        Task<bool> VerifyAccessibilityIssueAsync(Guid id, string testingNotes, string verifiedBy);
        Task<Dictionary<string, object>> GetAccessibilityStatsAsync();
        Task<Dictionary<string, object>> GetWCAGComplianceReportAsync(string? pageUrl = null);
        Task<List<AccessibilityIssueDto>> GetCriticalAccessibilityIssuesAsync();
        Task<bool> BulkUpdateAccessibilityIssuesAsync(List<Guid> issueIds, string status, string updatedBy);
    }
}
