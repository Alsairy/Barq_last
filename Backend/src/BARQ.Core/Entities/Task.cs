using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("Tasks")]
    public class Task : BaseEntity
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;
        
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
        public DateTime? CompletedDate { get; set; }
        
        public int EstimatedHours { get; set; } = 0;
        public int ActualHours { get; set; } = 0;
        public decimal ProgressPercentage { get; set; } = 0;
        
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

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
        
        [ForeignKey("AssignedToId")]
        public virtual ApplicationUser? AssignedTo { get; set; }
        
        [ForeignKey("CreatedBy")]
        public virtual ApplicationUser? Creator { get; set; }
        
        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }
        
        [ForeignKey("ParentTaskId")]
        public virtual Task? ParentTask { get; set; }
        
        public virtual ICollection<Task> SubTasks { get; set; } = new List<Task>();
        public virtual ICollection<TaskExecution> TaskExecutions { get; set; } = new List<TaskExecution>();
        public virtual ICollection<TaskDocument> TaskDocuments { get; set; } = new List<TaskDocument>();
        public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();
    }
}
