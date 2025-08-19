using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.Entities
{
    public class Translation : BaseEntity
    {
        [Required]
        [MaxLength(10)]
        public string LanguageCode { get; set; } = string.Empty; // en, ar, etc.
        
        [Required]
        [MaxLength(200)]
        public string Key { get; set; } = string.Empty; // translation key
        
        [Required]
        [MaxLength(5000)]
        public string Value { get; set; } = string.Empty; // translated text
        
        [MaxLength(100)]
        public string? Category { get; set; } // UI, Email, Validation, etc.
        
        [MaxLength(100)]
        public string? Context { get; set; } // additional context for translators
        
        public bool IsPlural { get; set; } = false;
        
        [MaxLength(5000)]
        public string? PluralValue { get; set; } // plural form for languages that need it
        
        public bool IsApproved { get; set; } = false;
        
        [MaxLength(100)]
        public string? ApprovedBy { get; set; }
        
        public DateTime? ApprovedAt { get; set; }
        
        [MaxLength(100)]
        public string? TranslatedBy { get; set; }
        
        public DateTime? TranslatedAt { get; set; }
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        [MaxLength(50)]
        public string? Region { get; set; } // for regional variations (en-US, en-GB, ar-SA, etc.)
        
        public int Priority { get; set; } = 0; // for ordering translations
        
        [MaxLength(2000)]
        public string? Metadata { get; set; } // JSON for additional data
    }
}
