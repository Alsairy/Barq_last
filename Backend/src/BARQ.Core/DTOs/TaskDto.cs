using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.DTOs
{
    public class TaskDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string TaskType { get; set; } = string.Empty;
        public Guid? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public Guid? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public Guid? ParentTaskId { get; set; }
        public string? ParentTaskName { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public int EstimatedHours { get; set; }
        public int ActualHours { get; set; }
        public decimal ProgressPercentage { get; set; }
        public string? Requirements { get; set; }
        public string? AcceptanceCriteria { get; set; }
        public string? Notes { get; set; }
        public string? Tags { get; set; }
        public bool IsRecurring { get; set; }
        public string? RecurrencePattern { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<TaskDto> SubTasks { get; set; } = new();
        public List<DocumentDto> Documents { get; set; } = new();
    }

    public class CreateTaskRequest
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(2000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Draft";
        
        [Required]
        [MaxLength(50)]
        public string Priority { get; set; } = "Medium";
        
        [Required]
        [MaxLength(100)]
        public string TaskType { get; set; } = string.Empty;
        
        public Guid? AssignedToId { get; set; }
        public Guid? ProjectId { get; set; }
        public Guid? ParentTaskId { get; set; }
        
        public DateTime? DueDate { get; set; }
        public DateTime? StartDate { get; set; }
        
        public int EstimatedHours { get; set; } = 0;
        
        [MaxLength(2000)]
        public string? Requirements { get; set; }
        
        [MaxLength(2000)]
        public string? AcceptanceCriteria { get; set; }
        
        [MaxLength(5000)]
        public string? Notes { get; set; }
        
        [MaxLength(1000)]
        public string? Tags { get; set; }
        
        public bool IsRecurring { get; set; } = false;
        
        [MaxLength(500)]
        public string? RecurrencePattern { get; set; }
    }

    public class UpdateTaskRequest
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(2000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Priority { get; set; } = string.Empty;
        
        public Guid? AssignedToId { get; set; }
        
        public DateTime? DueDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        
        public int EstimatedHours { get; set; }
        public int ActualHours { get; set; }
        public decimal ProgressPercentage { get; set; }
        
        [MaxLength(2000)]
        public string? Requirements { get; set; }
        
        [MaxLength(2000)]
        public string? AcceptanceCriteria { get; set; }
        
        [MaxLength(5000)]
        public string? Notes { get; set; }
        
        [MaxLength(1000)]
        public string? Tags { get; set; }
        
        public bool IsRecurring { get; set; }
        
        [MaxLength(500)]
        public string? RecurrencePattern { get; set; }
    }

    public class TaskListRequest
    {
        public Guid? ProjectId { get; set; }
        public Guid? AssignedToId { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? TaskType { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        public string? SearchTerm { get; set; }
        public string? Tags { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "CreatedAt";
        public string? SortDirection { get; set; } = "desc";
    }
}
