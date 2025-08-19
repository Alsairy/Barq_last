using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("Documents")]
    public class Document : BaseEntity
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string DocumentType { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string FilePath { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string FileExtension { get; set; } = string.Empty;
        
        public long FileSize { get; set; } = 0;
        
        [MaxLength(100)]
        public string? MimeType { get; set; }
        
        [MaxLength(500)]
        public string? FileHash { get; set; }
        
        public Guid? ProjectId { get; set; }
        public Guid? TemplateId { get; set; }
        
        [MaxLength(100)]
        public new string? Version { get; set; }
        
        public bool IsTemplate { get; set; } = false;
        public bool IsPublic { get; set; } = false;
        
        [MaxLength(1000)]
        public string? Tags { get; set; }
        
        [MaxLength(2000)]
        public string? Metadata { get; set; }
        
        public DateTime? ExpiryDate { get; set; }
        public int DownloadCount { get; set; } = 0;

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
        
        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }
        
        [ForeignKey("TemplateId")]
        public virtual Template? Template { get; set; }
        
        public virtual ICollection<TaskDocument> TaskDocuments { get; set; } = new List<TaskDocument>();
    }
}
