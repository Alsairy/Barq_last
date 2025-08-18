using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("TaskDocuments")]
    public class TaskDocument : BaseEntity
    {
        [Required]
        public Guid TaskId { get; set; }
        
        [Required]
        public Guid DocumentId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string RelationType { get; set; } = "Attachment";
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        public bool IsRequired { get; set; } = false;
        public int SortOrder { get; set; } = 0;

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
        
        [ForeignKey("TaskId")]
        public virtual Task Task { get; set; } = null!;
        
        [ForeignKey("DocumentId")]
        public virtual Document Document { get; set; } = null!;
    }
}
