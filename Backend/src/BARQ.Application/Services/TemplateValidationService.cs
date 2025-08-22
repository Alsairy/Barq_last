using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.Entities;
using BARQ.Core.Services;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BARQ.Application.Services
{
    public class TemplateValidationService : ITemplateValidationService
    {
        private readonly BarqDbContext _context;
        private readonly ITenantProvider _tenantProvider;
        private readonly ILogger<TemplateValidationService> _logger;

        public TemplateValidationService(
            BarqDbContext context,
            ITenantProvider tenantProvider,
            ILogger<TemplateValidationService> logger)
        {
            _context = context;
            _tenantProvider = tenantProvider;
            _logger = logger;
        }

        public async Task<TemplateValidationResult> ValidateTemplateAsync(Guid templateId)
        {
            try
            {
                var template = await _context.Templates
                    .Where(t => t.TenantId == _tenantProvider.GetTenantId() && t.Id == templateId)
                    .FirstOrDefaultAsync();
                if (template == null)
                {
                    return new TemplateValidationResult
                    {
                        IsValid = false,
                        Errors = new List<string> { "Template not found" },
                        ValidatedAt = DateTime.UtcNow
                    };
                }

                return await ValidateTemplateContentAsync(template.Content, template.TemplateType ?? "Unknown");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating template {TemplateId}", templateId);
                return new TemplateValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { "Validation failed due to internal error" },
                    ValidatedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<TemplateValidationResult> ValidateTemplateContentAsync(string templateContent, string templateType)
        {
            var result = new TemplateValidationResult
            {
                ValidatedAt = DateTime.UtcNow,
                ValidationVersion = "1.0"
            };

            try
            {
                var constraints = await GetActiveConstraintsAsync();
                var violations = new List<ConstraintViolation>();

                foreach (var constraint in constraints.Where(c => c.ConstraintType == "Template" || c.ConstraintType == templateType))
                {
                    var violation = await ValidateAgainstConstraintAsync(templateContent, templateType, constraint);
                    if (violation != null)
                    {
                        violations.Add(violation);
                        
                        if (violation.Severity == "Error")
                        {
                            result.Errors.Add($"{constraint.Name}: {violation.ViolationDetails}");
                        }
                        else if (violation.Severity == "Warning")
                        {
                            result.Warnings.Add($"{constraint.Name}: {violation.ViolationDetails}");
                        }
                    }
                }

                result.IsValid = result.Errors.Count == 0;

                _logger.LogInformation("Template validation completed: TemplateType={TemplateType}, IsValid={IsValid}, ErrorCount={ErrorCount}, WarningCount={WarningCount}", 
                    templateType, result.IsValid, result.Errors.Count, result.Warnings.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating template content");
                result.IsValid = false;
                result.Errors.Add("Validation failed due to internal error");
                return result;
            }
        }

        public async Task<bool> TestConnectionAsync(string connectionString, string connectionType)
        {
            try
            {
                switch (connectionType.ToLower())
                {
                    case "sqlserver":
                        return await TestSqlServerConnectionAsync(connectionString);
                    case "redis":
                        return await TestRedisConnectionAsync(connectionString);
                    case "http":
                    case "https":
                        return await TestHttpConnectionAsync(connectionString);
                    default:
                        _logger.LogWarning("Unknown connection type: {ConnectionType}", connectionType);
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection {ConnectionType}: {ConnectionString}", connectionType, connectionString);
                return false;
            }
        }

        public async Task<IEnumerable<ConstraintViolation>> GetViolationsAsync(Guid? templateId = null, Guid? taskId = null)
        {
            var query = _context.ConstraintViolations
                .Include(cv => cv.Constraint)
                .Include(cv => cv.Template)
                .Include(cv => cv.Task)
                .Include(cv => cv.ResolvedByUser)
                .Where(cv => cv.TenantId == _tenantProvider.GetTenantId() && 
                            (!templateId.HasValue || cv.TemplateId == templateId) &&
                            (!taskId.HasValue || cv.TaskId == taskId));

            return await query.OrderByDescending(cv => cv.CreatedAt).ToListAsync();
        }

        public async Task<bool> ResolveViolationAsync(Guid violationId, string resolutionNotes)
        {
            try
            {
                var violation = await _context.ConstraintViolations
                    .Where(cv => cv.TenantId == _tenantProvider.GetTenantId() && cv.Id == violationId)
                    .FirstOrDefaultAsync();
                if (violation == null) return false;

                violation.IsResolved = true;
                violation.ResolvedAt = DateTime.UtcNow;
                violation.ResolutionNotes = resolutionNotes;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Constraint violation resolved: {ViolationId}, Notes: {ResolutionNotes}", violationId, resolutionNotes);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving violation {ViolationId}", violationId);
                return false;
            }
        }

        public async Task<IEnumerable<TechnologyConstraint>> GetActiveConstraintsAsync()
        {
            return await _context.TechnologyConstraints
                .Where(tc => tc.TenantId == _tenantProvider.GetTenantId() && tc.IsActive)
                .OrderBy(tc => tc.Priority)
                .ToListAsync();
        }

        public async Task<TechnologyConstraint> CreateConstraintAsync(CreateTechnologyConstraintRequest request)
        {
            var constraint = new TechnologyConstraint
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantProvider.GetTenantId(),
                Name = request.Name,
                Description = request.Description,
                ConstraintType = request.ConstraintType,
                AllowedValues = request.AllowedValues,
                DeniedValues = request.DeniedValues,
                Priority = request.Priority,
                ValidationRule = request.ValidationRule,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.TechnologyConstraints.Add(constraint);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Technology constraint created: {ConstraintId}, Name: {Name}", constraint.Id, constraint.Name);

            return constraint;
        }

        public async Task<TechnologyConstraint> UpdateConstraintAsync(Guid constraintId, UpdateTechnologyConstraintRequest request)
        {
            var constraint = await _context.TechnologyConstraints
                .Where(tc => tc.TenantId == _tenantProvider.GetTenantId() && tc.Id == constraintId)
                .FirstOrDefaultAsync();
            if (constraint == null) throw new ArgumentException("Constraint not found");

            if (!string.IsNullOrEmpty(request.Name)) constraint.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Description)) constraint.Description = request.Description;
            if (!string.IsNullOrEmpty(request.ConstraintType)) constraint.ConstraintType = request.ConstraintType;
            if (!string.IsNullOrEmpty(request.AllowedValues)) constraint.AllowedValues = request.AllowedValues;
            if (!string.IsNullOrEmpty(request.DeniedValues)) constraint.DeniedValues = request.DeniedValues;
            if (request.IsActive.HasValue) constraint.IsActive = request.IsActive.Value;
            if (request.Priority.HasValue) constraint.Priority = request.Priority.Value;
            if (!string.IsNullOrEmpty(request.ValidationRule)) constraint.ValidationRule = request.ValidationRule;

            constraint.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Technology constraint updated: {ConstraintId}, Name: {Name}", constraint.Id, constraint.Name);

            return constraint;
        }

        public async Task<bool> DeleteConstraintAsync(Guid constraintId)
        {
            try
            {
                var constraint = await _context.TechnologyConstraints
                    .Where(tc => tc.TenantId == _tenantProvider.GetTenantId() && tc.Id == constraintId)
                    .FirstOrDefaultAsync();
                if (constraint == null) return false;

                constraint.IsDeleted = true;
                constraint.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Technology constraint deleted: {ConstraintId}", constraintId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting constraint {ConstraintId}", constraintId);
                return false;
            }
        }

        private async Task<ConstraintViolation?> ValidateAgainstConstraintAsync(string templateContent, string templateType, TechnologyConstraint constraint)
        {
            try
            {
                var deniedValues = string.IsNullOrEmpty(constraint.DeniedValues) 
                    ? new string[0] 
                    : JsonSerializer.Deserialize<string[]>(constraint.DeniedValues) ?? new string[0];

                var allowedValues = string.IsNullOrEmpty(constraint.AllowedValues) 
                    ? new string[0] 
                    : JsonSerializer.Deserialize<string[]>(constraint.AllowedValues) ?? new string[0];

                foreach (var deniedValue in deniedValues)
                {
                    if (templateContent.Contains(deniedValue, StringComparison.OrdinalIgnoreCase))
                    {
                        return new ConstraintViolation
                        {
                            Id = Guid.NewGuid(),
                            ConstraintId = constraint.Id,
                            ViolationType = "Template",
                            ViolationDetails = $"Template contains denied value: {deniedValue}",
                            Severity = "Error",
                            CreatedAt = DateTime.UtcNow
                        };
                    }
                }

                if (allowedValues.Length > 0)
                {
                    var hasAllowedValue = allowedValues.Any(av => templateContent.Contains(av, StringComparison.OrdinalIgnoreCase));
                    if (!hasAllowedValue)
                    {
                        return new ConstraintViolation
                        {
                            Id = Guid.NewGuid(),
                            ConstraintId = constraint.Id,
                            ViolationType = "Template",
                            ViolationDetails = $"Template must contain at least one allowed value: {string.Join(", ", allowedValues)}",
                            Severity = "Warning",
                            CreatedAt = DateTime.UtcNow
                        };
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating against constraint {ConstraintId}", constraint.Id);
                return null;
            }
        }

        private async System.Threading.Tasks.Task<bool> TestSqlServerConnectionAsync(string connectionString)
        {
            await System.Threading.Tasks.Task.Delay(100);
            return !string.IsNullOrEmpty(connectionString) && connectionString.Contains("Server=");
        }

        private async System.Threading.Tasks.Task<bool> TestRedisConnectionAsync(string connectionString)
        {
            await System.Threading.Tasks.Task.Delay(100);
            return !string.IsNullOrEmpty(connectionString) && (connectionString.Contains("localhost") || connectionString.Contains("redis"));
        }

        private async System.Threading.Tasks.Task<bool> TestHttpConnectionAsync(string connectionString)
        {
            await System.Threading.Tasks.Task.Delay(100);
            return Uri.TryCreate(connectionString, UriKind.Absolute, out _);
        }
    }
}
