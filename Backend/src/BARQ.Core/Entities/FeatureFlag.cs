using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class FeatureFlag : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        public bool IsEnabled { get; set; } = false;
        
        [Required]
        [MaxLength(50)]
        public string Environment { get; set; } = "Development"; // Development, Staging, Production, All
        
        [MaxLength(100)]
        public string? Category { get; set; } // Authentication, Workflow, Billing, etc.
        
        public DateTime? EnabledAt { get; set; }
        
        public DateTime? DisabledAt { get; set; }
        
        [MaxLength(100)]
        public string? EnabledBy { get; set; }
        
        [MaxLength(100)]
        public string? DisabledBy { get; set; }
        
        [MaxLength(2000)]
        public string? ImpactDescription { get; set; }
        
        [MaxLength(1000)]
        public string? RolloutStrategy { get; set; } // JSON for gradual rollout config
        
        public int RolloutPercentage { get; set; } = 0; // 0-100 for gradual rollout
        
        [MaxLength(2000)]
        public string? TargetAudience { get; set; } // JSON for targeting rules
        
        public bool RequiresRestart { get; set; } = false;
        
        public bool IsSystemFlag { get; set; } = false; // Cannot be deleted
        
        public int Priority { get; set; } = 0; // For ordering in UI
        
        public virtual ICollection<FeatureFlagHistory> History { get; set; } = new List<FeatureFlagHistory>();
    }
}
