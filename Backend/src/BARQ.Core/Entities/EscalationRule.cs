using BARQ.Core.Entities;

namespace BARQ.Core.Entities;

public class EscalationRule : TenantEntity
{
    public Guid SlaPolicyId { get; set; }
    public int Level { get; set; }
    public int TriggerAfterMinutes { get; set; }
    public string ActionType { get; set; } = string.Empty; // Notify, Reassign, AutoTransition, Webhook
    public string ActionConfig { get; set; } = "{}"; // JSON configuration
    public bool IsActive { get; set; } = true;
    
    public virtual SlaPolicy SlaPolicy { get; set; } = null!;
    public virtual ICollection<EscalationAction> EscalationActions { get; set; } = new List<EscalationAction>();
}

public class EscalationAction : TenantEntity
{
    public Guid SlaViolationId { get; set; }
    public Guid? EscalationRuleId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string ActionConfig { get; set; } = "{}";
    public DateTime ExecutedAt { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Executed, Failed
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
    public DateTime? NextRetryAt { get; set; }
    
    public virtual SlaViolation SlaViolation { get; set; } = null!;
    public virtual EscalationRule? EscalationRule { get; set; }
}
