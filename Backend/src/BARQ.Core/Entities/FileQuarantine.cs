using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class FileQuarantine : BaseEntity
    {
        [Required]
        public Guid FileAttachmentId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Reason { get; set; } = string.Empty;
        
        [MaxLength(2000)]
        public string? Details { get; set; }
        
        [Required]
        public Guid QuarantinedBy { get; set; }
        
        public DateTime QuarantinedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ReviewedAt { get; set; }
        
        public Guid? ReviewedBy { get; set; }
        
        [MaxLength(50)]
        public string Status { get; set; } = "Quarantined"; // Quarantined, Released, Deleted
        
        [MaxLength(1000)]
        public string? ReviewNotes { get; set; }
        
        public virtual FileAttachment FileAttachment { get; set; } = null!;
        public virtual ApplicationUser QuarantinedByUser { get; set; } = null!;
        public virtual ApplicationUser? ReviewedByUser { get; set; }
    }
}
