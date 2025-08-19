using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SystemTask = System.Threading.Tasks.Task;

namespace BARQ.Application.Services
{
    public class AccessibilityService : IAccessibilityService
    {
        private readonly BarqDbContext _context;
        private readonly ILogger<AccessibilityService> _logger;

        public AccessibilityService(BarqDbContext context, ILogger<AccessibilityService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResult<AccessibilityAuditDto>> GetAccessibilityAuditsAsync(ListRequest request)
        {
            try
            {
                var query = _context.AccessibilityAudits.Include(a => a.Issues).AsQueryable();

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(a => a.PageUrl.Contains(request.SearchTerm) ||
                                           a.PageTitle.Contains(request.SearchTerm) ||
                                           a.AuditType.Contains(request.SearchTerm));
                }

                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    query = request.SortDirection?.ToLower() == "desc"
                        ? query.OrderByDescending(a => EF.Property<object>(a, request.SortBy))
                        : query.OrderBy(a => EF.Property<object>(a, request.SortBy));
                }
                else
                {
                    query = query.OrderByDescending(a => a.AuditDate);
                }

                var totalCount = await query.CountAsync();
                var audits = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var auditDtos = audits.Select(MapToDto).ToList();

                return new PagedResult<AccessibilityAuditDto>
                {
                    Items = auditDtos,
                    Total = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accessibility audits");
                throw;
            }
        }

        public async Task<AccessibilityAuditDto?> GetAccessibilityAuditByIdAsync(Guid id)
        {
            try
            {
                var audit = await _context.AccessibilityAudits
                    .Include(a => a.Issues)
                    .FirstOrDefaultAsync(a => a.Id == id);

                return audit != null ? MapToDto(audit) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accessibility audit by ID: {Id}", id);
                throw;
            }
        }

        public async Task<List<AccessibilityAuditDto>> GetAccessibilityAuditsByPageAsync(string pageUrl)
        {
            try
            {
                var audits = await _context.AccessibilityAudits
                    .Include(a => a.Issues)
                    .Where(a => a.PageUrl == pageUrl)
                    .OrderByDescending(a => a.AuditDate)
                    .ToListAsync();

                return audits.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accessibility audits by page: {PageUrl}", pageUrl);
                throw;
            }
        }

        public async Task<AccessibilityAuditDto> CreateAccessibilityAuditAsync(CreateAccessibilityAuditRequest request, string createdBy)
        {
            try
            {
                var audit = new AccessibilityAudit
                {
                    Id = Guid.NewGuid(),
                    PageUrl = request.PageUrl,
                    PageTitle = request.PageTitle,
                    AuditType = request.AuditType,
                    WCAGLevel = request.WCAGLevel,
                    AuditDate = DateTime.UtcNow,
                    AuditedBy = createdBy,
                    Tool = request.Tool,
                    ToolVersion = request.ToolVersion,
                    Summary = request.Summary,
                    Recommendations = request.Recommendations,
                    NextAuditDate = request.NextAuditDate,
                    Notes = request.Notes,
                    IsPublic = request.IsPublic,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                _context.AccessibilityAudits.Add(audit);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Accessibility audit created: {PageUrl} by {CreatedBy}", 
                    request.PageUrl, createdBy);

                return MapToDto(audit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating accessibility audit");
                throw;
            }
        }

        public async Task<AccessibilityAuditDto?> UpdateAccessibilityAuditAsync(Guid id, CreateAccessibilityAuditRequest request, string updatedBy)
        {
            try
            {
                var audit = await _context.AccessibilityAudits.FindAsync(id);
                if (audit == null)
                {
                    return null;
                }

                audit.PageTitle = request.PageTitle;
                audit.AuditType = request.AuditType;
                audit.WCAGLevel = request.WCAGLevel;
                audit.Tool = request.Tool;
                audit.ToolVersion = request.ToolVersion;
                audit.Summary = request.Summary;
                audit.Recommendations = request.Recommendations;
                audit.NextAuditDate = request.NextAuditDate;
                audit.Notes = request.Notes;
                audit.IsPublic = request.IsPublic;
                audit.UpdatedAt = DateTime.UtcNow;
                audit.UpdatedBy = updatedBy;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Accessibility audit updated: {Id} by {UpdatedBy}", id, updatedBy);

                return MapToDto(audit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating accessibility audit: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAccessibilityAuditAsync(Guid id, string deletedBy)
        {
            try
            {
                var audit = await _context.AccessibilityAudits.FindAsync(id);
                if (audit == null)
                {
                    return false;
                }

                audit.IsDeleted = true;
                audit.UpdatedAt = DateTime.UtcNow;
                audit.UpdatedBy = deletedBy;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Accessibility audit deleted: {Id} by {DeletedBy}", id, deletedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting accessibility audit: {Id}", id);
                throw;
            }
        }

        public async Task<AccessibilityIssueDto> CreateAccessibilityIssueAsync(CreateAccessibilityIssueRequest request, string createdBy)
        {
            try
            {
                var issue = new AccessibilityIssue
                {
                    Id = Guid.NewGuid(),
                    AccessibilityAuditId = Guid.Parse(request.AccessibilityAuditId),
                    Title = request.Title,
                    Description = request.Description,
                    Severity = request.Severity,
                    IssueType = request.IssueType,
                    WCAGCriterion = request.WCAGCriterion,
                    WCAGCriterionName = request.WCAGCriterionName,
                    WCAGLevel = request.WCAGLevel,
                    Element = request.Element,
                    ElementContext = request.ElementContext,
                    PageLocation = request.PageLocation,
                    CurrentValue = request.CurrentValue,
                    ExpectedValue = request.ExpectedValue,
                    HowToFix = request.HowToFix,
                    CodeExample = request.CodeExample,
                    Priority = request.Priority,
                    AssignedTo = request.AssignedTo,
                    RequiresUserTesting = request.RequiresUserTesting,
                    RequiresScreenReaderTesting = request.RequiresScreenReaderTesting,
                    UserImpact = request.UserImpact,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                if (!string.IsNullOrEmpty(request.AssignedTo))
                {
                    issue.AssignedAt = DateTime.UtcNow;
                }

                _context.AccessibilityIssues.Add(issue);

                await UpdateAuditStatisticsAsync(issue.AccessibilityAuditId);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Accessibility issue created: {Title} for audit {AuditId} by {CreatedBy}", 
                    request.Title, request.AccessibilityAuditId, createdBy);

                return MapIssueToDto(issue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating accessibility issue");
                throw;
            }
        }

        public async Task<AccessibilityIssueDto?> UpdateAccessibilityIssueAsync(Guid id, CreateAccessibilityIssueRequest request, string updatedBy)
        {
            try
            {
                var issue = await _context.AccessibilityIssues.FindAsync(id);
                if (issue == null)
                {
                    return null;
                }

                var oldSeverity = issue.Severity;

                issue.Title = request.Title;
                issue.Description = request.Description;
                issue.Severity = request.Severity;
                issue.IssueType = request.IssueType;
                issue.WCAGCriterion = request.WCAGCriterion;
                issue.WCAGCriterionName = request.WCAGCriterionName;
                issue.WCAGLevel = request.WCAGLevel;
                issue.Element = request.Element;
                issue.ElementContext = request.ElementContext;
                issue.PageLocation = request.PageLocation;
                issue.CurrentValue = request.CurrentValue;
                issue.ExpectedValue = request.ExpectedValue;
                issue.HowToFix = request.HowToFix;
                issue.CodeExample = request.CodeExample;
                issue.Priority = request.Priority;
                issue.RequiresUserTesting = request.RequiresUserTesting;
                issue.RequiresScreenReaderTesting = request.RequiresScreenReaderTesting;
                issue.UserImpact = request.UserImpact;
                issue.UpdatedAt = DateTime.UtcNow;
                issue.UpdatedBy = updatedBy;

                if (!string.IsNullOrEmpty(request.AssignedTo) && issue.AssignedTo != request.AssignedTo)
                {
                    issue.AssignedTo = request.AssignedTo;
                    issue.AssignedAt = DateTime.UtcNow;
                }

                if (oldSeverity != issue.Severity)
                {
                    await UpdateAuditStatisticsAsync(issue.AccessibilityAuditId);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Accessibility issue updated: {Id} by {UpdatedBy}", id, updatedBy);

                return MapIssueToDto(issue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating accessibility issue: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAccessibilityIssueAsync(Guid id, string deletedBy)
        {
            try
            {
                var issue = await _context.AccessibilityIssues.FindAsync(id);
                if (issue == null)
                {
                    return false;
                }

                var auditId = issue.AccessibilityAuditId;

                issue.IsDeleted = true;
                issue.UpdatedAt = DateTime.UtcNow;
                issue.UpdatedBy = deletedBy;

                await UpdateAuditStatisticsAsync(auditId);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Accessibility issue deleted: {Id} by {DeletedBy}", id, deletedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting accessibility issue: {Id}", id);
                throw;
            }
        }

        public async Task<PagedResult<AccessibilityIssueDto>> GetAccessibilityIssuesAsync(Guid auditId, ListRequest request)
        {
            try
            {
                var query = _context.AccessibilityIssues.Where(i => i.AccessibilityAuditId == auditId).AsQueryable();

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(i => i.Title.Contains(request.SearchTerm) ||
                                           i.Description.Contains(request.SearchTerm) ||
                                           i.Severity.Contains(request.SearchTerm));
                }

                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    query = request.SortDirection?.ToLower() == "desc"
                        ? query.OrderByDescending(i => EF.Property<object>(i, request.SortBy))
                        : query.OrderBy(i => EF.Property<object>(i, request.SortBy));
                }
                else
                {
                    query = query.OrderByDescending(i => i.CreatedAt);
                }

                var totalCount = await query.CountAsync();
                var issues = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var issueDtos = issues.Select(MapIssueToDto).ToList();

                return new PagedResult<AccessibilityIssueDto>
                {
                    Items = issueDtos,
                    Total = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accessibility issues for audit: {AuditId}", auditId);
                throw;
            }
        }

        public async Task<List<AccessibilityIssueDto>> GetAccessibilityIssuesBySeverityAsync(string severity)
        {
            try
            {
                var issues = await _context.AccessibilityIssues
                    .Where(i => i.Severity == severity && i.Status == "Open")
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                return issues.Select(MapIssueToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accessibility issues by severity: {Severity}", severity);
                throw;
            }
        }

        public async Task<List<AccessibilityIssueDto>> GetAccessibilityIssuesByStatusAsync(string status)
        {
            try
            {
                var issues = await _context.AccessibilityIssues
                    .Where(i => i.Status == status)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                return issues.Select(MapIssueToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accessibility issues by status: {Status}", status);
                throw;
            }
        }

        public async Task<bool> AssignAccessibilityIssueAsync(Guid id, string assignedTo, string assignedBy)
        {
            try
            {
                var issue = await _context.AccessibilityIssues.FindAsync(id);
                if (issue == null)
                {
                    return false;
                }

                issue.AssignedTo = assignedTo;
                issue.AssignedAt = DateTime.UtcNow;
                issue.Status = "In Progress";
                issue.UpdatedAt = DateTime.UtcNow;
                issue.UpdatedBy = assignedBy;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Accessibility issue assigned: {Id} to {AssignedTo} by {AssignedBy}", 
                    id, assignedTo, assignedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning accessibility issue: {Id}", id);
                throw;
            }
        }

        public async Task<bool> ResolveAccessibilityIssueAsync(Guid id, string fixNotes, string resolvedBy)
        {
            try
            {
                var issue = await _context.AccessibilityIssues.FindAsync(id);
                if (issue == null)
                {
                    return false;
                }

                issue.Status = "Fixed";
                issue.FixedAt = DateTime.UtcNow;
                issue.FixedBy = resolvedBy;
                issue.FixNotes = fixNotes;
                issue.UpdatedAt = DateTime.UtcNow;
                issue.UpdatedBy = resolvedBy;

                await UpdateAuditStatisticsAsync(issue.AccessibilityAuditId);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Accessibility issue resolved: {Id} by {ResolvedBy}", id, resolvedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving accessibility issue: {Id}", id);
                throw;
            }
        }

        public async Task<bool> VerifyAccessibilityIssueAsync(Guid id, string testingNotes, string verifiedBy)
        {
            try
            {
                var issue = await _context.AccessibilityIssues.FindAsync(id);
                if (issue == null || issue.Status != "Fixed")
                {
                    return false;
                }

                issue.VerifiedAt = DateTime.UtcNow;
                issue.VerifiedBy = verifiedBy;
                issue.TestingNotes = testingNotes;
                issue.UpdatedAt = DateTime.UtcNow;
                issue.UpdatedBy = verifiedBy;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Accessibility issue verified: {Id} by {VerifiedBy}", id, verifiedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying accessibility issue: {Id}", id);
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetAccessibilityStatsAsync()
        {
            try
            {
                var totalAudits = await _context.AccessibilityAudits.CountAsync();
                var totalIssues = await _context.AccessibilityIssues.CountAsync();
                var openIssues = await _context.AccessibilityIssues.CountAsync(i => i.Status == "Open");
                var criticalIssues = await _context.AccessibilityIssues.CountAsync(i => i.Severity == "Critical" && i.Status == "Open");

                var issuesBySeverity = await _context.AccessibilityIssues
                    .Where(i => i.Status == "Open")
                    .GroupBy(i => i.Severity)
                    .Select(g => new { Severity = g.Key, Count = g.Count() })
                    .ToListAsync();

                var issuesByType = await _context.AccessibilityIssues
                    .Where(i => i.Status == "Open")
                    .GroupBy(i => i.IssueType)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync();

                var avgComplianceScore = await _context.AccessibilityAudits
                    .Where(a => a.Status == "Completed")
                    .AverageAsync(a => (double?)a.ComplianceScore) ?? 0;

                return new Dictionary<string, object>
                {
                    ["TotalAudits"] = totalAudits,
                    ["TotalIssues"] = totalIssues,
                    ["OpenIssues"] = openIssues,
                    ["CriticalIssues"] = criticalIssues,
                    ["IssuesBySeverity"] = issuesBySeverity,
                    ["IssuesByType"] = issuesByType,
                    ["AverageComplianceScore"] = Math.Round(avgComplianceScore, 2)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accessibility stats");
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetWCAGComplianceReportAsync(string? pageUrl = null)
        {
            try
            {
                var query = _context.AccessibilityAudits.AsQueryable();

                if (!string.IsNullOrEmpty(pageUrl))
                {
                    query = query.Where(a => a.PageUrl == pageUrl);
                }

                var audits = await query
                    .Where(a => a.Status == "Completed")
                    .ToListAsync();

                var complianceByLevel = audits
                    .GroupBy(a => a.WCAGLevel)
                    .ToDictionary(g => g.Key, g => new
                    {
                        AuditCount = g.Count(),
                        AverageScore = Math.Round(g.Average(a => a.ComplianceScore), 2),
                        PassingAudits = g.Count(a => a.ComplianceScore >= 80)
                    });

                var criteriaCompliance = await _context.AccessibilityIssues
                    .Where(i => audits.Select(a => a.Id).Contains(i.AccessibilityAuditId))
                    .GroupBy(i => i.WCAGCriterion)
                    .Select(g => new
                    {
                        Criterion = g.Key,
                        TotalIssues = g.Count(),
                        OpenIssues = g.Count(i => i.Status == "Open"),
                        CriticalIssues = g.Count(i => i.Severity == "Critical")
                    })
                    .OrderByDescending(x => x.TotalIssues)
                    .Take(20)
                    .ToListAsync();

                return new Dictionary<string, object>
                {
                    ["PageUrl"] = pageUrl ?? "All Pages",
                    ["TotalAudits"] = audits.Count,
                    ["ComplianceByLevel"] = complianceByLevel,
                    ["CriteriaCompliance"] = criteriaCompliance,
                    ["GeneratedAt"] = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting WCAG compliance report");
                throw;
            }
        }

        public async Task<List<AccessibilityIssueDto>> GetCriticalAccessibilityIssuesAsync()
        {
            try
            {
                var issues = await _context.AccessibilityIssues
                    .Where(i => i.Severity == "Critical" && i.Status == "Open")
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                return issues.Select(MapIssueToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting critical accessibility issues");
                throw;
            }
        }

        public async Task<bool> BulkUpdateAccessibilityIssuesAsync(List<Guid> issueIds, string status, string updatedBy)
        {
            try
            {
                var issues = await _context.AccessibilityIssues
                    .Where(i => issueIds.Contains(i.Id))
                    .ToListAsync();

                foreach (var issue in issues)
                {
                    issue.Status = status;
                    issue.UpdatedAt = DateTime.UtcNow;
                    issue.UpdatedBy = updatedBy;

                    if (status == "Fixed")
                    {
                        issue.FixedAt = DateTime.UtcNow;
                        issue.FixedBy = updatedBy;
                    }
                }

                var auditIds = issues.Select(i => i.AccessibilityAuditId).Distinct();
                foreach (var auditId in auditIds)
                {
                    await UpdateAuditStatisticsAsync(auditId);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Bulk updated {Count} accessibility issues to status {Status} by {UpdatedBy}", 
                    issues.Count, status, updatedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk updating accessibility issues");
                throw;
            }
        }

        private async SystemTask UpdateAuditStatisticsAsync(Guid auditId)
        {
            var audit = await _context.AccessibilityAudits.FindAsync(auditId);
            if (audit == null) return;

            var issues = await _context.AccessibilityIssues
                .Where(i => i.AccessibilityAuditId == auditId && !i.IsDeleted)
                .ToListAsync();

            audit.TotalIssues = issues.Count;
            audit.CriticalIssues = issues.Count(i => i.Severity == "Critical");
            audit.SeriousIssues = issues.Count(i => i.Severity == "Serious");
            audit.ModerateIssues = issues.Count(i => i.Severity == "Moderate");
            audit.MinorIssues = issues.Count(i => i.Severity == "Minor");

            var maxPossibleScore = 100.0;
            var deductions = (audit.CriticalIssues * 20) + (audit.SeriousIssues * 10) + 
                           (audit.ModerateIssues * 5) + (audit.MinorIssues * 2);
            
            audit.ComplianceScore = Math.Max(0, maxPossibleScore - deductions);

            if (audit.TotalIssues == 0)
            {
                audit.Status = "Completed";
                audit.ComplianceScore = 100;
            }
            else if (audit.CriticalIssues == 0 && audit.SeriousIssues == 0)
            {
                audit.Status = "Completed";
            }
            else
            {
                audit.Status = "In Progress";
            }

            audit.UpdatedAt = DateTime.UtcNow;
            audit.UpdatedBy = "System";
        }

        private static AccessibilityAuditDto MapToDto(AccessibilityAudit audit)
        {
            return new AccessibilityAuditDto
            {
                Id = audit.Id.ToString(),
                PageUrl = audit.PageUrl,
                PageTitle = audit.PageTitle,
                AuditType = audit.AuditType,
                WCAGLevel = audit.WCAGLevel,
                AuditDate = audit.AuditDate,
                AuditedBy = audit.AuditedBy,
                Tool = audit.Tool,
                ToolVersion = audit.ToolVersion,
                TotalIssues = audit.TotalIssues,
                CriticalIssues = audit.CriticalIssues,
                SeriousIssues = audit.SeriousIssues,
                ModerateIssues = audit.ModerateIssues,
                MinorIssues = audit.MinorIssues,
                ComplianceScore = audit.ComplianceScore,
                Status = audit.Status,
                Summary = audit.Summary,
                Recommendations = audit.Recommendations,
                NextAuditDate = audit.NextAuditDate,
                Notes = audit.Notes,
                IsPublic = audit.IsPublic,
                CreatedAt = audit.CreatedAt,
                UpdatedAt = audit.UpdatedAt,
                Issues = audit.Issues?.Select(MapIssueToDto).ToList() ?? new List<AccessibilityIssueDto>()
            };
        }

        private static AccessibilityIssueDto MapIssueToDto(AccessibilityIssue issue)
        {
            return new AccessibilityIssueDto
            {
                Id = issue.Id.ToString(),
                AccessibilityAuditId = issue.AccessibilityAuditId.ToString(),
                Title = issue.Title,
                Description = issue.Description,
                Severity = issue.Severity,
                IssueType = issue.IssueType,
                WCAGCriterion = issue.WCAGCriterion,
                WCAGCriterionName = issue.WCAGCriterionName,
                WCAGLevel = issue.WCAGLevel,
                Element = issue.Element,
                ElementContext = issue.ElementContext,
                PageLocation = issue.PageLocation,
                CurrentValue = issue.CurrentValue,
                ExpectedValue = issue.ExpectedValue,
                HowToFix = issue.HowToFix,
                CodeExample = issue.CodeExample,
                Status = issue.Status,
                Priority = issue.Priority,
                AssignedTo = issue.AssignedTo,
                AssignedAt = issue.AssignedAt,
                FixedAt = issue.FixedAt,
                FixedBy = issue.FixedBy,
                FixNotes = issue.FixNotes,
                VerifiedAt = issue.VerifiedAt,
                VerifiedBy = issue.VerifiedBy,
                TestingNotes = issue.TestingNotes,
                RequiresUserTesting = issue.RequiresUserTesting,
                RequiresScreenReaderTesting = issue.RequiresScreenReaderTesting,
                UserImpact = issue.UserImpact,
                CreatedAt = issue.CreatedAt,
                UpdatedAt = issue.UpdatedAt
            };
        }
    }
}
