using BARQ.Core.Entities;

namespace BARQ.Core.Entities
{
    public class ConstraintViolation : BaseEntity
    {
        public Guid? TemplateId { get; set; }
        public Template? Template { get; set; }
        public Guid? TaskId { get; set; }
        public Task? Task { get; set; }
        public Guid ConstraintId { get; set; }
        public TechnologyConstraint Constraint { get; set; } = null!;
        public string ViolationType { get; set; } = string.Empty; // "Template", "Stack", "Technology"
        public string ViolationDetails { get; set; } = string.Empty; // JSON details
        public string Severity { get; set; } = string.Empty; // "Error", "Warning", "Info"
        public bool IsResolved { get; set; } = false;
        public DateTime? ResolvedAt { get; set; }
        public Guid? ResolvedBy { get; set; }
        public ApplicationUser? ResolvedByUser { get; set; }
        public string ResolutionNotes { get; set; } = string.Empty;
    }
}
