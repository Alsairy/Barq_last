using BARQ.Core.Entities;

namespace BARQ.Core.Entities;

public class SlaPolicy : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public int ResponseTimeHours { get; set; }
    public int ResolutionTimeHours { get; set; }
    public Guid? BusinessCalendarId { get; set; }
    public bool IsActive { get; set; } = true;
    
    public virtual BusinessCalendar? BusinessCalendar { get; set; }
    public virtual ICollection<SlaViolation> SlaViolations { get; set; } = new List<SlaViolation>();
    public virtual ICollection<EscalationRule> EscalationRules { get; set; } = new List<EscalationRule>();
}

public class SlaViolation : TenantEntity
{
    public Guid SlaPolicyId { get; set; }
    public Guid TaskId { get; set; }
    public string ViolationType { get; set; } = string.Empty; // Response, Resolution
    public DateTime ViolationTime { get; set; }
    public DateTime? ResolvedTime { get; set; }
    public string Status { get; set; } = "Open"; // Open, Resolved, Escalated
    public string? Resolution { get; set; }
    public int EscalationLevel { get; set; } = 0;
    
    public virtual SlaPolicy SlaPolicy { get; set; } = null!;
    public virtual Task Task { get; set; } = null!;
    public virtual ICollection<EscalationAction> EscalationActions { get; set; } = new List<EscalationAction>();
}
