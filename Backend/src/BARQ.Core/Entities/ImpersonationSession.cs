using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class ImpersonationSession : BaseEntity
    {
        [Required]
        public Guid AdminUserId { get; set; }
        
        [Required]
        public Guid TargetUserId { get; set; }
        
        [Required]
        public Guid TenantId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string SessionToken { get; set; } = string.Empty;
        
        [Required]
        public DateTime StartedAt { get; set; }
        
        public DateTime? EndedAt { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Active"; // Active, Ended, Expired
        
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? TicketNumber { get; set; }
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        public DateTime ExpiresAt { get; set; }
        
        [MaxLength(100)]
        public string? EndedBy { get; set; }
        
        [MaxLength(500)]
        public string? EndReason { get; set; }
        
        [MaxLength(50)]
        public string? IpAddress { get; set; }
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        public int ActionCount { get; set; } = 0;
        
        public DateTime? LastActivityAt { get; set; }
        
        [MaxLength(2000)]
        public string? Metadata { get; set; } // JSON for session details
        
        public bool IsAudited { get; set; } = true;
        
        public virtual ApplicationUser AdminUser { get; set; } = null!;
        public virtual ApplicationUser TargetUser { get; set; } = null!;
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual ICollection<ImpersonationAction> Actions { get; set; } = new List<ImpersonationAction>();
    }
}
