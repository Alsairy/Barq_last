using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class Language : BaseEntity
    {
        [Required]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty; // ISO 639-1 code (en, ar, etc.)
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // English name
        
        [Required]
        [MaxLength(100)]
        public string NativeName { get; set; } = string.Empty; // Native name (العربية, English)
        
        [Required]
        [MaxLength(10)]
        public string Direction { get; set; } = "ltr"; // ltr or rtl
        
        public bool IsEnabled { get; set; } = true;
        
        public bool IsDefault { get; set; } = false;
        
        public int SortOrder { get; set; } = 0;
        
        [MaxLength(10)]
        public string? Region { get; set; } // US, GB, SA, etc.
        
        [MaxLength(20)]
        public string? CultureCode { get; set; } // en-US, ar-SA, etc.
        
        [MaxLength(10)]
        public string? DateFormat { get; set; } // MM/dd/yyyy, dd/MM/yyyy, etc.
        
        [MaxLength(10)]
        public string? TimeFormat { get; set; } // 12h, 24h
        
        [MaxLength(10)]
        public string? NumberFormat { get; set; } // 1,234.56 or 1.234,56
        
        [MaxLength(10)]
        public string? CurrencySymbol { get; set; } // $, ر.س, etc.
        
        [MaxLength(10)]
        public string? CurrencyPosition { get; set; } // before, after
        
        public double CompletionPercentage { get; set; } = 0.0; // translation completion %
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        [MaxLength(2000)]
        public string? Metadata { get; set; } // JSON for additional language settings
        
        public virtual ICollection<Translation> Translations { get; set; } = new List<Translation>();
        public virtual ICollection<UserLanguagePreference> UserPreferences { get; set; } = new List<UserLanguagePreference>();
    }
}
