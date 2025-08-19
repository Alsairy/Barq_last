using Microsoft.AspNetCore.Identity;

namespace BARQ.Core.Entities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public Guid TenantId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string? TimeZone { get; set; }
        public string? PreferredLanguage { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        
        public virtual ICollection<UserLanguagePreference> LanguagePreferences { get; set; } = new List<UserLanguagePreference>();
        
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
