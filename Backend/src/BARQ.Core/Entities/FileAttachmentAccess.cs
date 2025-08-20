using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class FileAttachmentAccess : BaseEntity
    {
        [Required]
        public Guid FileAttachmentId { get; set; }
        
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string AccessType { get; set; } = string.Empty; // View, Download, Edit, Delete
        
        public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(45)]
        public string? IpAddress { get; set; }
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        [MaxLength(1000)]
        public string? AccessDetails { get; set; }
        
        public virtual FileAttachment FileAttachment { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
