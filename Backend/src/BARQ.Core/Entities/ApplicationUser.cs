using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("Users")]
    public class ApplicationUser : IdentityUser<Guid>, ITenantEntity, IAuditable
    {
        public Guid TenantId { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string LastName { get; set; } = string.Empty;
        
        [MaxLength(255)]
        public string? DisplayName { get; set; }
        
        [MaxLength(255)]
        public string? JobTitle { get; set; }
        
        [MaxLength(255)]
        public string? Department { get; set; }
        
        [MaxLength(100)]
        public string? EmployeeId { get; set; }
        
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginDate { get; set; }
        public DateTime? PasswordChangedDate { get; set; }
        public bool RequirePasswordChange { get; set; } = false;
        
        [MaxLength(1000)]
        public string? ProfileImageUrl { get; set; }
        
        [MaxLength(2000)]
        public string? Bio { get; set; }
        
        [MaxLength(100)]
        public string? TimeZone { get; set; }
        
        [MaxLength(10)]
        public string? Language { get; set; }
        
        [MaxLength(10)]
        public string? PreferredLanguage { get; set; }
        
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
        public virtual ICollection<Task> CreatedTasks { get; set; } = new List<Task>();
        public virtual ICollection<Task> AssignedTasks { get; set; } = new List<Task>();
        public virtual ICollection<Project> OwnedProjects { get; set; } = new List<Project>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<UserLanguagePreference> LanguagePreferences { get; set; } = new List<UserLanguagePreference>();
        
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
