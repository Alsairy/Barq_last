using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("TaskExecutions")]
    public class TaskExecution : BaseEntity
    {
        [Required]
        public Guid TaskId { get; set; }
        
        public Guid? AgentId { get; set; }
        public Guid? ProviderId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";
        
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        [MaxLength(5000)]
        public string? Input { get; set; }
        
        [MaxLength(10000)]
        public string? Output { get; set; }
        
        [MaxLength(2000)]
        public string? ErrorMessage { get; set; }
        
        public int ExecutionTimeMs { get; set; } = 0;
        public decimal? Cost { get; set; }
        
        public int TokensUsed { get; set; } = 0;
        public int InputTokens { get; set; } = 0;
        public int OutputTokens { get; set; } = 0;
        
        [MaxLength(100)]
        public string? Model { get; set; }
        
        [MaxLength(2000)]
        public string? Metadata { get; set; }
        
        public int RetryCount { get; set; } = 0;
        public bool IsSuccessful { get; set; } = false;

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
        
        [ForeignKey("TaskId")]
        public virtual Task Task { get; set; } = null!;
        
        [ForeignKey("AgentId")]
        public virtual AIAgent? Agent { get; set; }
        
        [ForeignKey("ProviderId")]
        public virtual AIProvider? Provider { get; set; }
    }
}
