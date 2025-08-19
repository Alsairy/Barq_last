using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class TenantStateHistory : BaseEntity
    {
        [Required]
        public Guid TenantStateId { get; set; }
        
        [Required]
        public Guid TenantId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string PreviousStatus { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string NewStatus { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Reason { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string ChangedBy { get; set; } = string.Empty;
        
        [Required]
        public DateTime ChangedAt { get; set; }
        
        [MaxLength(2000)]
        public string? ChangeDetails { get; set; } // JSON for detailed changes
        
        public bool WasHealthy { get; set; }
        
        public bool IsHealthy { get; set; }
        
        public virtual TenantState TenantState { get; set; } = null!;
        public virtual Tenant Tenant { get; set; } = null!;
    }
}
