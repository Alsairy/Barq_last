using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("Roles")]
    public class Role : IdentityRole<Guid>, ITenantEntity, IAuditable
    {
        public Guid TenantId { get; set; }
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        public bool IsSystemRole { get; set; } = false;
        public bool IsActive { get; set; } = true;
        
        [MaxLength(100)]
        public string? Category { get; set; }
        
        public int Priority { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public Guid? DeletedBy { get; set; }
        public int Version { get; set; } = 1;

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
        
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
