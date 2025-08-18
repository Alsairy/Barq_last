using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.DTOs
{
    public class ProjectDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public Guid? OwnerId { get; set; }
        public string? OwnerName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? ActualStartDate { get; set; }
        public DateTime? ActualEndDate { get; set; }
        public decimal Budget { get; set; }
        public decimal ActualCost { get; set; }
        public decimal ProgressPercentage { get; set; }
        public string? Objectives { get; set; }
        public string? Scope { get; set; }
        public string? Deliverables { get; set; }
        public string? Stakeholders { get; set; }
        public string? Risks { get; set; }
        public string? Tags { get; set; }
        public bool IsTemplate { get; set; }
        public Guid? TemplateId { get; set; }
        public string? TemplateName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<TaskDto> Tasks { get; set; } = new();
        public List<DocumentDto> Documents { get; set; } = new();
    }

    public class CreateProjectRequest
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(2000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Planning";
        
        [Required]
        [MaxLength(50)]
        public string Priority { get; set; } = "Medium";
        
        public Guid? OwnerId { get; set; }
        
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        public decimal Budget { get; set; } = 0;
        
        [MaxLength(1000)]
        public string? Objectives { get; set; }
        
        [MaxLength(2000)]
        public string? Scope { get; set; }
        
        [MaxLength(1000)]
        public string? Deliverables { get; set; }
        
        [MaxLength(1000)]
        public string? Stakeholders { get; set; }
        
        [MaxLength(1000)]
        public string? Risks { get; set; }
        
        [MaxLength(1000)]
        public string? Tags { get; set; }
        
        public bool IsTemplate { get; set; } = false;
        public Guid? TemplateId { get; set; }
    }

    public class UpdateProjectRequest
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
        
        public Guid? OwnerId { get; set; }
        
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? ActualStartDate { get; set; }
        public DateTime? ActualEndDate { get; set; }
        
        public decimal Budget { get; set; }
        public decimal ActualCost { get; set; }
        public decimal ProgressPercentage { get; set; }
        
        [MaxLength(1000)]
        public string? Objectives { get; set; }
        
        [MaxLength(2000)]
        public string? Scope { get; set; }
        
        [MaxLength(1000)]
        public string? Deliverables { get; set; }
        
        [MaxLength(1000)]
        public string? Stakeholders { get; set; }
        
        [MaxLength(1000)]
        public string? Risks { get; set; }
        
        [MaxLength(1000)]
        public string? Tags { get; set; }
    }
}
