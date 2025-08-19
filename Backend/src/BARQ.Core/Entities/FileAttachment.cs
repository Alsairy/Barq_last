using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class FileAttachment : BaseEntity
    {
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;
        
        public long FileSize { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string StoragePath { get; set; } = string.Empty;
        
        [MaxLength(64)]
        public string? FileHash { get; set; }
        
        [Required]
        public Guid UploadedBy { get; set; }
        
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Scanning, Clean, Quarantined, Deleted
        
        [MaxLength(50)]
        public string? ScanResult { get; set; }
        
        public DateTime? ScanCompletedAt { get; set; }
        
        [MaxLength(1000)]
        public string? ScanDetails { get; set; }
        
        public bool IsPublic { get; set; } = false;
        
        public DateTime? ExpiresAt { get; set; }
        
        [MaxLength(2000)]
        public string? Metadata { get; set; } // JSON for additional file metadata
        
        [MaxLength(500)]
        public string? ThumbnailPath { get; set; }
        
        [MaxLength(500)]
        public string? PreviewPath { get; set; }
        
        public virtual ApplicationUser UploadedByUser { get; set; } = null!;
        public virtual Tenant? Tenant { get; set; }
        public virtual ICollection<FileAttachmentAccess> AccessRecords { get; set; } = new List<FileAttachmentAccess>();
    }
}
