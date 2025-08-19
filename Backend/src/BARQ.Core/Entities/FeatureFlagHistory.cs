using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class FeatureFlagHistory : BaseEntity
    {
        [Required]
        public Guid FeatureFlagId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty; // Enabled, Disabled, Created, Updated, Deleted
        
        [Required]
        public bool PreviousValue { get; set; }
        
        [Required]
        public bool NewValue { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string ChangedBy { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Reason { get; set; }
        
        [MaxLength(2000)]
        public string? ChangeDetails { get; set; } // JSON for detailed changes
        
        [Required]
        public DateTime ChangedAt { get; set; }
        
        [MaxLength(50)]
        public string? Environment { get; set; }
        
        public virtual FeatureFlag FeatureFlag { get; set; } = null!;
    }
}
