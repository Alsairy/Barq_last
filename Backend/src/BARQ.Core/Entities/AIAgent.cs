using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("AIAgents")]
    public class AIAgent : BaseEntity
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        public Guid ProviderId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string AgentType { get; set; } = string.Empty;
        
        [MaxLength(2000)]
        public string? Configuration { get; set; }
        
        [MaxLength(5000)]
        public string? SystemPrompt { get; set; }
        
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        
        public int Priority { get; set; } = 0;
        public decimal? CostPerRequest { get; set; }
        
        [MaxLength(100)]
        public string? Model { get; set; }
        
        public int MaxTokens { get; set; } = 4000;
        public decimal Temperature { get; set; } = 0.7m;
        
        [MaxLength(1000)]
        public string? Capabilities { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
        
        [ForeignKey("ProviderId")]
        public virtual AIProvider Provider { get; set; } = null!;
        
        public virtual ICollection<TaskExecution> TaskExecutions { get; set; } = new List<TaskExecution>();
    }
}
