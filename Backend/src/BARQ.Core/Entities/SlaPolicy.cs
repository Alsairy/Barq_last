using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities;

public class SlaPolicy : BaseEntity
{
    [Required]
    [StringLength(50)]
    public string Priority { get; set; } = string.Empty;

    [Required]
    public int ResponseTimeHours { get; set; }

    [Required]
    public int ResolutionTimeHours { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Description { get; set; }

    public virtual ICollection<SlaViolation> SlaViolations { get; set; } = new List<SlaViolation>();
}
