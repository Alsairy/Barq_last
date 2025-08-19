using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class ReportTemplate : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty; // AuditLog, UserActivity, SystemMetrics, etc.
        
        [Required]
        public string TemplateContent { get; set; } = string.Empty; // JSON template definition
        
        [Required]
        public new Guid CreatedBy { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public bool IsSystemTemplate { get; set; } = false;
        
        [MaxLength(100)]
        public string? Category { get; set; }
        
        [MaxLength(2000)]
        public string? DefaultFilters { get; set; } // JSON for default filters
        
        public int UsageCount { get; set; } = 0;
        
        public DateTime? LastUsedAt { get; set; }
        
        public virtual ApplicationUser CreatedByUser { get; set; } = null!;
        public virtual Tenant? Tenant { get; set; }
        public virtual ICollection<AuditReport> Reports { get; set; } = new List<AuditReport>();
    }
}
