using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class EscalationRule : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string TaskType { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string EscalationType { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string ActionType { get; set; } = string.Empty;
        
        [Required]
        public int Priority { get; set; }
        
        [Required]
        public int DelayMinutes { get; set; }
        
        [StringLength(1000)]
        public string? Conditions { get; set; }
        
        [StringLength(1000)]
        public string? ActionParameters { get; set; }
        
        [Required]
        public bool IsActive { get; set; } = true;
        
        public virtual ICollection<EscalationAction> EscalationActions { get; set; } = new List<EscalationAction>();
    }
}
