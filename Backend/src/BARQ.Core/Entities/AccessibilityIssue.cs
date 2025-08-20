using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class AccessibilityIssue : BaseEntity
    {
        [Required]
        public Guid AccessibilityAuditId { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Severity { get; set; } = string.Empty; // Critical, Serious, Moderate, Minor
        
        [Required]
        [MaxLength(50)]
        public string IssueType { get; set; } = string.Empty; // Color Contrast, Keyboard Navigation, Screen Reader, etc.
        
        [Required]
        [MaxLength(20)]
        public string WCAGCriterion { get; set; } = string.Empty; // 1.4.3, 2.1.1, etc.
        
        [MaxLength(100)]
        public string? WCAGCriterionName { get; set; } // Contrast (Minimum), Keyboard, etc.
        
        [Required]
        [MaxLength(20)]
        public string WCAGLevel { get; set; } = "AA"; // A, AA, AAA
        
        [MaxLength(500)]
        public string? Element { get; set; } // CSS selector or description
        
        [MaxLength(1000)]
        public string? ElementContext { get; set; } // surrounding HTML context
        
        [MaxLength(200)]
        public string? PageLocation { get; set; } // specific location on page
        
        [MaxLength(2000)]
        public string? CurrentValue { get; set; } // current problematic value
        
        [MaxLength(2000)]
        public string? ExpectedValue { get; set; } // what it should be
        
        [MaxLength(2000)]
        public string? HowToFix { get; set; } // remediation instructions
        
        [MaxLength(1000)]
        public string? CodeExample { get; set; } // example of correct code
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Open"; // Open, In Progress, Fixed, Won't Fix, False Positive
        
        [MaxLength(50)]
        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical
        
        [MaxLength(100)]
        public string? AssignedTo { get; set; }
        
        public DateTime? AssignedAt { get; set; }
        
        public DateTime? FixedAt { get; set; }
        
        [MaxLength(100)]
        public string? FixedBy { get; set; }
        
        [MaxLength(1000)]
        public string? FixNotes { get; set; }
        
        public DateTime? VerifiedAt { get; set; }
        
        [MaxLength(100)]
        public string? VerifiedBy { get; set; }
        
        [MaxLength(1000)]
        public string? TestingNotes { get; set; }
        
        public bool RequiresUserTesting { get; set; } = false;
        
        public bool RequiresScreenReaderTesting { get; set; } = false;
        
        [MaxLength(2000)]
        public string? UserImpact { get; set; } // description of impact on users
        
        [MaxLength(2000)]
        public string? Metadata { get; set; } // JSON for additional issue data
        
        public virtual AccessibilityAudit AccessibilityAudit { get; set; } = null!;
    }
}
