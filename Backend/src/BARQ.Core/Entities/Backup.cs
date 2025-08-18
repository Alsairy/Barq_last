using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("Backups")]
    public class Backup : BaseEntity
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string BackupType { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";
        
        [Required]
        [MaxLength(1000)]
        public string FilePath { get; set; } = string.Empty;
        
        public long FileSize { get; set; } = 0;
        
        [MaxLength(500)]
        public string? FileHash { get; set; }
        
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        public bool IsCompressed { get; set; } = false;
        public bool IsEncrypted { get; set; } = false;
        
        [MaxLength(2000)]
        public string? Configuration { get; set; }
        
        [MaxLength(2000)]
        public string? ErrorMessage { get; set; }
        
        public int RetentionDays { get; set; } = 30;
        public DateTime? ExpiryDate { get; set; }
        
        [MaxLength(2000)]
        public string? Metadata { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
    }
}
