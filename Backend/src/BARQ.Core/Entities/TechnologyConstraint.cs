using BARQ.Core.Entities;

namespace BARQ.Core.Entities
{
    public class TechnologyConstraint : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ConstraintType { get; set; } = string.Empty; // "Technology", "Template", "Stack"
        public string AllowedValues { get; set; } = string.Empty; // JSON array of allowed values
        public string DeniedValues { get; set; } = string.Empty; // JSON array of denied values
        public bool IsActive { get; set; } = true;
        public int Priority { get; set; } = 0;
        public string ValidationRule { get; set; } = string.Empty; // JSON validation rule
    }
}
