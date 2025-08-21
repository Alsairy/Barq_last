using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("SlaViolations")]
    public class SlaViolation : BaseEntity
    {
        [Required]
        public Guid TaskId { get; set; }
        
        [Required]
        public Guid SlaId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string ViolationType { get; set; } = string.Empty; // Warning, Breach, Critical
        
        [Required]
        public DateTime ViolationTime { get; set; }
        
        [Required]
        public DateTime DueTime { get; set; }
        
        public TimeSpan DelayDuration { get; set; }
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Open"; // Open, Acknowledged, Resolved
        
        public DateTime? AcknowledgedAt { get; set; }
        
        public Guid? AcknowledgedBy { get; set; }
        
        public DateTime? ResolvedAt { get; set; }
        
        public Guid? ResolvedBy { get; set; }
        
        [MaxLength(2000)]
        public string? ResolutionNotes { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
        
        public bool EscalationTriggered { get; set; } = false;
        
        public DateTime? EscalationTime { get; set; }
        
        [MaxLength(2000)]
        public string? Metadata { get; set; } // JSON for additional data
        
        [ForeignKey("TaskId")]
        public virtual BARQ.Core.Entities.Task Task { get; set; } = null!;
        
        [ForeignKey("AcknowledgedBy")]
        public virtual ApplicationUser? AcknowledgedByUser { get; set; }
        
        [ForeignKey("ResolvedBy")]
        public virtual ApplicationUser? ResolvedByUser { get; set; }
    }
}
