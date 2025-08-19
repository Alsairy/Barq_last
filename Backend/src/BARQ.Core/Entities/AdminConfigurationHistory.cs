using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("AdminConfigurationHistory")]
    public class AdminConfigurationHistory : BaseEntity
    {
        [Required]
        public Guid AdminConfigurationId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;
        
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        
        [MaxLength(1000)]
        public string? ChangeReason { get; set; }
        
        [MaxLength(255)]
        public Guid? ChangedBy { get; set; }
        
        public DateTime ChangeDate { get; set; } = DateTime.UtcNow;
        
        [MaxLength(2000)]
        public string? AdditionalData { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
        
        [ForeignKey("AdminConfigurationId")]
        public virtual AdminConfiguration AdminConfiguration { get; set; } = null!;
        
        [ForeignKey("ChangedBy")]
        public virtual ApplicationUser? ChangedByUser { get; set; }
    }
}
