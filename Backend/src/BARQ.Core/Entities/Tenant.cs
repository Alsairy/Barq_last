using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("Tenants")]
    public class Tenant : IEntity, IAuditable
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string DisplayName { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        [MaxLength(500)]
        public string? ContactEmail { get; set; }
        
        [MaxLength(100)]
        public string? ContactPhone { get; set; }
        
        [MaxLength(1000)]
        public string? Address { get; set; }
        
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        
        [MaxLength(100)]
        public string? SubscriptionTier { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public string? DeletedBy { get; set; }
        public int Version { get; set; } = 1;

        public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
        public virtual ICollection<AIProvider> AIProviders { get; set; } = new List<AIProvider>();
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
