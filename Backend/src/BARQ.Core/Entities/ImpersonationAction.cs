using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class ImpersonationAction : BaseEntity
    {
        [Required]
        public Guid ImpersonationSessionId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string ActionType { get; set; } = string.Empty; // View, Create, Update, Delete, Execute, etc.
        
        [Required]
        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty; // Task, Project, User, etc.
        
        [MaxLength(100)]
        public string? EntityId { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;
        
        [MaxLength(2000)]
        public string? Details { get; set; } // JSON for action details
        
        [Required]
        public DateTime PerformedAt { get; set; }
        
        [MaxLength(50)]
        public string? IpAddress { get; set; }
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        [MaxLength(50)]
        public string? HttpMethod { get; set; }
        
        [MaxLength(500)]
        public string? RequestPath { get; set; }
        
        [MaxLength(2000)]
        public string? RequestBody { get; set; }
        
        [MaxLength(2000)]
        public string? ResponseBody { get; set; }
        
        public int ResponseStatusCode { get; set; }
        
        public long ResponseTimeMs { get; set; }
        
        [MaxLength(50)]
        public string? RiskLevel { get; set; } // Low, Medium, High, Critical
        
        public bool RequiresApproval { get; set; } = false;
        
        public bool IsApproved { get; set; } = false;
        
        [MaxLength(100)]
        public string? ApprovedBy { get; set; }
        
        public DateTime? ApprovedAt { get; set; }
        
        public virtual ImpersonationSession ImpersonationSession { get; set; } = null!;
    }
}
