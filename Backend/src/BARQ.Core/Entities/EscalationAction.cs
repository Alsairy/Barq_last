using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class EscalationAction : BaseEntity
    {
        [Required]
        public Guid TaskId { get; set; }
        
        [Required]
        public Guid RuleId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ActionType { get; set; } = string.Empty;
        
        [Required]
        public DateTime ExecutedAt { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";
        
        [StringLength(1000)]
        public string? Details { get; set; }
        
        public virtual Task? Task { get; set; }
        public virtual EscalationRule? Rule { get; set; }
    }
}
