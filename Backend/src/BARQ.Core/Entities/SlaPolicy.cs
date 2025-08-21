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
