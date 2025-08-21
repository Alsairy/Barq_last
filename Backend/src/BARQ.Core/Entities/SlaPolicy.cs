using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class SlaPolicy : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string TaskType { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        public double MaxHours { get; set; }
        
        [Required]
        public double WarningHours { get; set; }
        
        [Required]
        public bool IsActive { get; set; } = true;
        
        [StringLength(100)]
        public string? BusinessCalendarId { get; set; }
        
        public virtual ICollection<SlaViolation> SlaViolations { get; set; } = new List<SlaViolation>();
    }
}
