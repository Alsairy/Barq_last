using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("Workflows")]
    public class Workflow : BaseEntity
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string WorkflowType { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;
        
        [Required]
        public string ProcessDefinition { get; set; } = string.Empty;
        
        [MaxLength(255)]
        public string? ProcessDefinitionKey { get; set; }
        
        [MaxLength(100)]
        public new string? Version { get; set; }
        
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        
        [MaxLength(2000)]
        public string? Configuration { get; set; }
        
        [MaxLength(1000)]
        public string? TriggerConditions { get; set; }
        
        public int Priority { get; set; } = 0;
        public int TimeoutMinutes { get; set; } = 60;
        
        [MaxLength(1000)]
        public string? Tags { get; set; }
        
        public int ExecutionCount { get; set; } = 0;
        public DateTime? LastExecuted { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
        
        public virtual ICollection<WorkflowInstance> WorkflowInstances { get; set; } = new List<WorkflowInstance>();
    }
}
