using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class AuditReport : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        public Guid GeneratedBy { get; set; }
        
        [Required]
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Generating, Completed, Failed
        
        [MaxLength(500)]
        public string? FilePath { get; set; }
        
        [MaxLength(100)]
        public string Format { get; set; } = "PDF"; // PDF, Excel, CSV
        
        [MaxLength(2000)]
        public string? Filters { get; set; } // JSON for report filters
        
        public long? FileSizeBytes { get; set; }
        
        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }
        
        public DateTime? CompletedAt { get; set; }
        
        public DateTime? ExpiresAt { get; set; }
        
        public bool IsScheduled { get; set; } = false;
        
        [MaxLength(100)]
        public string? ScheduleCron { get; set; }
        
        public DateTime? NextRunAt { get; set; }
        
        public virtual ApplicationUser GeneratedByUser { get; set; } = null!;
        public virtual Tenant? Tenant { get; set; }
    }
}
