using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class UserLanguagePreference : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        public Guid LanguageId { get; set; }
        
        [Required]
        [MaxLength(10)]
        public string LanguageCode { get; set; } = string.Empty;
        
        public bool IsDefault { get; set; } = false;
        
        [MaxLength(20)]
        public string? DateFormat { get; set; }
        
        [MaxLength(20)]
        public string? TimeFormat { get; set; }
        
        [MaxLength(20)]
        public string? NumberFormat { get; set; }
        
        [MaxLength(50)]
        public string? Timezone { get; set; }
        
        [MaxLength(10)]
        public string? CurrencyCode { get; set; }
        
        public bool UseRTL { get; set; } = false;
        
        public bool HighContrast { get; set; } = false;
        
        public bool LargeText { get; set; } = false;
        
        public bool ReducedMotion { get; set; } = false;
        
        public bool ScreenReaderOptimized { get; set; } = false;
        
        [MaxLength(20)]
        public string? KeyboardNavigation { get; set; } // Standard, Enhanced, Custom
        
        [MaxLength(1000)]
        public string? AccessibilitySettings { get; set; } // JSON for custom a11y settings
        
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual Language Language { get; set; } = null!;
    }
}
