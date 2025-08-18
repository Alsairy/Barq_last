using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("Templates")]
    public class Template : BaseEntity
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string TemplateType { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        [MaxLength(2000)]
        public string? Variables { get; set; }
        
        [MaxLength(2000)]
        public string? ValidationRules { get; set; }
        
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        public bool IsSystemTemplate { get; set; } = false;
        
        [MaxLength(100)]
        public new string? Version { get; set; }
        
        public int UsageCount { get; set; } = 0;
        
        [MaxLength(1000)]
        public string? Tags { get; set; }
        
        [MaxLength(2000)]
        public string? Metadata { get; set; }
        
        public Guid? ParentTemplateId { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
        
        [ForeignKey("ParentTemplateId")]
        public virtual Template? ParentTemplate { get; set; }
        
        public virtual ICollection<Template> ChildTemplates { get; set; } = new List<Template>();
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
