using BARQ.Core.Entities;

namespace BARQ.Core.Entities
{
    public class TemplateValidation : BaseEntity
    {
        public Guid TemplateId { get; set; }
        public Template Template { get; set; } = null!;
        public string ValidationStatus { get; set; } = string.Empty; // "Valid", "Invalid", "Pending"
        public string ValidationErrors { get; set; } = string.Empty; // JSON array of errors
        public string ValidationWarnings { get; set; } = string.Empty; // JSON array of warnings
        public DateTime ValidatedAt { get; set; }
        public Guid ValidatedBy { get; set; }
        public ApplicationUser ValidatedByUser { get; set; } = null!;
        public string ValidationVersion { get; set; } = string.Empty;
    }
}
