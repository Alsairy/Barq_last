using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("SystemConfigurations")]
    public class SystemConfiguration : IEntity, IAuditable
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid? TenantId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string Key { get; set; } = string.Empty;
        
        [Required]
        public string Value { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string DataType { get; set; } = "String";
        
        public bool IsEncrypted { get; set; } = false;
        public bool IsReadOnly { get; set; } = false;
        public bool IsSystemConfig { get; set; } = false;
        
        [MaxLength(2000)]
        public string? ValidationRules { get; set; }
        
        [MaxLength(500)]
        public string? DefaultValue { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public Guid? DeletedBy { get; set; }
        public int Version { get; set; } = 1;

        [ForeignKey("TenantId")]
        public virtual Tenant? Tenant { get; set; }
    }
}
