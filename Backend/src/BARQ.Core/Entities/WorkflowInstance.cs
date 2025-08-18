using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("WorkflowInstances")]
    public class WorkflowInstance : BaseEntity
    {
        [Required]
        public Guid WorkflowId { get; set; }
        
        [MaxLength(255)]
        public string? ProcessInstanceId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Running";
        
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        [MaxLength(2000)]
        public string? Input { get; set; }
        
        [MaxLength(5000)]
        public string? Output { get; set; }
        
        [MaxLength(2000)]
        public string? ErrorMessage { get; set; }
        
        public Guid? InitiatedById { get; set; }
        public Guid? CurrentAssigneeId { get; set; }
        
        [MaxLength(100)]
        public string? CurrentStep { get; set; }
        
        public decimal ProgressPercentage { get; set; } = 0;
        
        [MaxLength(50)]
        public string? Priority { get; set; } = "Medium";
        
        [MaxLength(2000)]
        public string? Variables { get; set; }
        
        [MaxLength(2000)]
        public string? Metadata { get; set; }
        
        public bool IsSuccessful { get; set; } = false;

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
        
        [ForeignKey("WorkflowId")]
        public virtual Workflow Workflow { get; set; } = null!;
        
        [ForeignKey("InitiatedById")]
        public virtual ApplicationUser? InitiatedBy { get; set; }
        
        [ForeignKey("CurrentAssigneeId")]
        public virtual ApplicationUser? CurrentAssignee { get; set; }
    }
}
