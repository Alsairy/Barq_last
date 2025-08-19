using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.DTOs
{
    public class WorkflowDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string WorkflowType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ProcessDefinition { get; set; } = string.Empty;
        public string? ProcessDefinitionKey { get; set; }
        public string? Version { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
        public int Priority { get; set; }
        public int TimeoutMinutes { get; set; }
        public string? Tags { get; set; }
        public int ExecutionCount { get; set; }
        public DateTime? LastExecuted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<WorkflowInstanceDto> Instances { get; set; } = new();
    }

    public class CreateWorkflowRequest
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
        public string? Version { get; set; }
        
        public bool IsDefault { get; set; } = false;
        
        [MaxLength(2000)]
        public string? Configuration { get; set; }
        
        [MaxLength(1000)]
        public string? TriggerConditions { get; set; }
        
        public int Priority { get; set; } = 0;
        public int TimeoutMinutes { get; set; } = 60;
        
        [MaxLength(1000)]
        public string? Tags { get; set; }
    }

    public class WorkflowInstanceDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid WorkflowId { get; set; }
        public string? WorkflowName { get; set; }
        public string? ProcessInstanceId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Guid? InitiatedById { get; set; }
        public string? InitiatedByName { get; set; }
        public Guid? CurrentAssigneeId { get; set; }
        public string? CurrentAssigneeName { get; set; }
        public string? CurrentStep { get; set; }
        public decimal ProgressPercentage { get; set; }
        public string? Priority { get; set; }
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class StartWorkflowRequest
    {
        [Required]
        public Guid WorkflowId { get; set; }
        
        [MaxLength(2000)]
        public string? Input { get; set; }
        
        [MaxLength(50)]
        public string? Priority { get; set; } = "Medium";
        
        [MaxLength(2000)]
        public string? Variables { get; set; }
    }
}
