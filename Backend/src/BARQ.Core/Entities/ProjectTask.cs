using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("ProjectTasks")]
    public class ProjectTask : BaseEntity
    {
        [Required]
        public Guid ProjectId { get; set; }
        
        [Required]
        public Guid TaskId { get; set; }
        
        public int SortOrder { get; set; } = 0;
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        public DateTime? AssignedDate { get; set; }
        public bool IsActive { get; set; } = true;

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
        
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; } = null!;
        
        [ForeignKey("TaskId")]
        public virtual Task Task { get; set; } = null!;
    }
}
