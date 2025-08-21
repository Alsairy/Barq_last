using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("SlaViolations")]
    public class SlaViolation : TenantEntity
    {
        [Required]
        public Guid SlaPolicyId { get; set; }
        
        [Required]
        public Guid TaskId { get; set; }
        
        [Required]
        [MaxLength(32)]
        public string ViolationType { get; set; } = string.Empty; // Response, Resolution
        
        [Required]
        public DateTime ViolationTime { get; set; }
        
        public DateTime? ResolvedTime { get; set; }
        
        [Required]
        [MaxLength(32)]
        public string Status { get; set; } = "Open"; // Open, Resolved, Escalated
        
        [MaxLength(2000)]
        public string? Resolution { get; set; }
        
        public int EscalationLevel { get; set; } = 0;
        
        [MaxLength(2000)]
        public string? Metadata { get; set; } // JSON for additional data
        
        [ForeignKey("SlaPolicyId")]
        public virtual SlaPolicy SlaPolicy { get; set; } = null!;
        
        [ForeignKey("TaskId")]
        public virtual BARQ.Core.Entities.Task Task { get; set; } = null!;
        
        public virtual ICollection<EscalationAction> EscalationActions { get; set; } = new List<EscalationAction>();
    }
}
