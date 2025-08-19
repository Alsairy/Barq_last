using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.DTOs
{
    public class DocumentDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string? MimeType { get; set; }
        public Guid? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public Guid? TemplateId { get; set; }
        public string? TemplateName { get; set; }
        public string? Version { get; set; }
        public bool IsTemplate { get; set; }
        public bool IsPublic { get; set; }
        public string? Tags { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int DownloadCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateDocumentRequest
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string DocumentType { get; set; } = string.Empty;
        
        public Guid? ProjectId { get; set; }
        public Guid? TemplateId { get; set; }
        
        [MaxLength(100)]
        public string? Version { get; set; }
        
        public bool IsTemplate { get; set; } = false;
        public bool IsPublic { get; set; } = false;
        
        [MaxLength(1000)]
        public string? Tags { get; set; }
        
        public DateTime? ExpiryDate { get; set; }
    }

    public class UpdateDocumentRequest
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string DocumentType { get; set; } = string.Empty;
        
        public Guid? ProjectId { get; set; }
        public Guid? TemplateId { get; set; }
        
        [MaxLength(100)]
        public string? Version { get; set; }
        
        public bool IsTemplate { get; set; }
        public bool IsPublic { get; set; }
        
        [MaxLength(1000)]
        public string? Tags { get; set; }
        
        public DateTime? ExpiryDate { get; set; }
    }
}
