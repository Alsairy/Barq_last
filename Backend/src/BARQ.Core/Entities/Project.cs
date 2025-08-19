using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("Projects")]
    public class Project : BaseEntity
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
        public DateTime? ActualStartDate { get; set; }
        public DateTime? ActualEndDate { get; set; }
        
        public decimal Budget { get; set; } = 0;
        public decimal ActualCost { get; set; } = 0;
        
        public decimal ProgressPercentage { get; set; } = 0;
        
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

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
        
        [ForeignKey("OwnerId")]
        public virtual ApplicationUser? Owner { get; set; }
        
        [ForeignKey("TemplateId")]
        public virtual Project? Template { get; set; }
        
        public virtual ICollection<Project> DerivedProjects { get; set; } = new List<Project>();
        public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
        public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
