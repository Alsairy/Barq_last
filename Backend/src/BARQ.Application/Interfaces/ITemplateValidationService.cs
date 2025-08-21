using BARQ.Core.DTOs;
using BARQ.Core.Entities;

namespace BARQ.Application.Interfaces
{
    public interface ITemplateValidationService
    {
        Task<TemplateValidationResult> ValidateTemplateAsync(Guid templateId);
        Task<TemplateValidationResult> ValidateTemplateContentAsync(string templateContent, string templateType);
        Task<bool> TestConnectionAsync(string connectionString, string connectionType);
        Task<IEnumerable<ConstraintViolation>> GetViolationsAsync(Guid? templateId = null, Guid? taskId = null);
        Task<bool> ResolveViolationAsync(Guid violationId, string resolutionNotes);
        Task<IEnumerable<TechnologyConstraint>> GetActiveConstraintsAsync();
        Task<TechnologyConstraint> CreateConstraintAsync(CreateTechnologyConstraintRequest request);
        Task<TechnologyConstraint> UpdateConstraintAsync(Guid constraintId, UpdateTechnologyConstraintRequest request);
        Task<bool> DeleteConstraintAsync(Guid constraintId);
    }

    public class TemplateValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public string ValidationVersion { get; set; } = string.Empty;
        public DateTime ValidatedAt { get; set; }
    }

    public class CreateTechnologyConstraintRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ConstraintType { get; set; } = string.Empty;
        public string AllowedValues { get; set; } = string.Empty;
        public string DeniedValues { get; set; } = string.Empty;
        public int Priority { get; set; } = 0;
        public string ValidationRule { get; set; } = string.Empty;
    }

    public class UpdateTechnologyConstraintRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ConstraintType { get; set; }
        public string? AllowedValues { get; set; }
        public string? DeniedValues { get; set; }
        public bool? IsActive { get; set; }
        public int? Priority { get; set; }
        public string? ValidationRule { get; set; }
    }
}
