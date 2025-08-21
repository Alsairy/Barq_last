using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class SlaViolation : BaseEntity
    {
        [Required]
        public Guid TaskId { get; set; }
        
        [Required]
        public DateTime ViolationTime { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Open";
        
        public DateTime? ResolvedAt { get; set; }
        
        [StringLength(500)]
        public string? Resolution { get; set; }
        
        public virtual Task? Task { get; set; }
    }
}
