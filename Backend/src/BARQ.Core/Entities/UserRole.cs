using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("UserRoles")]
    public class UserRole : IdentityUserRole<Guid>, IAuditable
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public bool IsActive { get; set; } = true;
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public string? DeletedBy { get; set; }
        public int Version { get; set; } = 1;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;
        
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!;
    }
}
