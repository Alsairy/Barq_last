using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class AccessibilityAudit : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string PageUrl { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string PageTitle { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string AuditType { get; set; } = string.Empty; // Manual, Automated, User Testing
        
        [Required]
        [MaxLength(20)]
        public string WCAGLevel { get; set; } = "AA"; // A, AA, AAA
        
        [Required]
        public DateTime AuditDate { get; set; }
        
        [MaxLength(100)]
        public string? AuditedBy { get; set; }
        
        [MaxLength(100)]
        public string? Tool { get; set; } // axe-core, WAVE, manual, etc.
        
        [MaxLength(20)]
        public string? ToolVersion { get; set; }
        
        public int TotalIssues { get; set; } = 0;
        
        public int CriticalIssues { get; set; } = 0;
        
        public int SeriousIssues { get; set; } = 0;
        
        public int ModerateIssues { get; set; } = 0;
        
        public int MinorIssues { get; set; } = 0;
        
        public double ComplianceScore { get; set; } = 0.0; // 0-100
        
        [MaxLength(50)]
        public string Status { get; set; } = "In Progress"; // In Progress, Completed, Failed
        
        [MaxLength(5000)]
        public string? Summary { get; set; }
        
        [MaxLength(10000)]
        public string? DetailedResults { get; set; } // JSON with detailed audit results
        
        [MaxLength(2000)]
        public string? Recommendations { get; set; }
        
        public DateTime? NextAuditDate { get; set; }
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        public bool IsPublic { get; set; } = false;
        
        [MaxLength(2000)]
        public string? Metadata { get; set; } // JSON for additional audit data
        
        public virtual ICollection<AccessibilityIssue> Issues { get; set; } = new List<AccessibilityIssue>();
    }
}
