using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.DTOs
{
    public class LanguageDto
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string NativeName { get; set; } = string.Empty;
        public string Direction { get; set; } = "ltr";
        public bool IsEnabled { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        public int SortOrder { get; set; } = 0;
        public string? Region { get; set; }
        public string? CultureCode { get; set; }
        public string? DateFormat { get; set; }
        public string? TimeFormat { get; set; }
        public string? NumberFormat { get; set; }
        public string? CurrencySymbol { get; set; }
        public string? CurrencyPosition { get; set; }
        public double CompletionPercentage { get; set; } = 0.0;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateLanguageRequest
    {
        [Required]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string NativeName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(10)]
        public string Direction { get; set; } = "ltr";
        
        public bool IsEnabled { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        public int SortOrder { get; set; } = 0;
        public string? Region { get; set; }
        public string? CultureCode { get; set; }
        public string? DateFormat { get; set; }
        public string? TimeFormat { get; set; }
        public string? NumberFormat { get; set; }
        public string? CurrencySymbol { get; set; }
        public string? CurrencyPosition { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateLanguageRequest
    {
        [MaxLength(100)]
        public string? Name { get; set; }
        
        [MaxLength(100)]
        public string? NativeName { get; set; }
        
        [MaxLength(10)]
        public string? Direction { get; set; }
        
        public bool? IsEnabled { get; set; }
        public bool? IsDefault { get; set; }
        public int? SortOrder { get; set; }
        public string? Region { get; set; }
        public string? CultureCode { get; set; }
        public string? DateFormat { get; set; }
        public string? TimeFormat { get; set; }
        public string? NumberFormat { get; set; }
        public string? CurrencySymbol { get; set; }
        public string? CurrencyPosition { get; set; }
        public string? Notes { get; set; }
    }

    public class TranslationDto
    {
        public string Id { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Context { get; set; }
        public bool IsPlural { get; set; } = false;
        public string? PluralValue { get; set; }
        public bool IsApproved { get; set; } = false;
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? TranslatedBy { get; set; }
        public DateTime? TranslatedAt { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Region { get; set; }
        public int Priority { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateTranslationRequest
    {
        [Required]
        [MaxLength(10)]
        public string LanguageCode { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string Key { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(5000)]
        public string Value { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? Category { get; set; }
        
        [MaxLength(100)]
        public string? Context { get; set; }
        
        public bool IsPlural { get; set; } = false;
        
        [MaxLength(5000)]
        public string? PluralValue { get; set; }
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        [MaxLength(50)]
        public string? Region { get; set; }
        
        public int Priority { get; set; } = 0;
    }

    public class UpdateTranslationRequest
    {
        [MaxLength(5000)]
        public string? Value { get; set; }
        
        [MaxLength(100)]
        public string? Category { get; set; }
        
        [MaxLength(100)]
        public string? Context { get; set; }
        
        public bool? IsPlural { get; set; }
        
        [MaxLength(5000)]
        public string? PluralValue { get; set; }
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        public bool? IsActive { get; set; }
        
        [MaxLength(50)]
        public string? Region { get; set; }
        
        public int? Priority { get; set; }
    }

    public class BulkTranslationRequest
    {
        [Required]
        [MaxLength(10)]
        public string LanguageCode { get; set; } = string.Empty;
        
        [Required]
        public List<TranslationKeyValue> Translations { get; set; } = new();
    }

    public class TranslationKeyValue
    {
        [Required]
        public string Key { get; set; } = string.Empty;
        
        [Required]
        public string Value { get; set; } = string.Empty;
        
        public string? Category { get; set; }
        public string? Context { get; set; }
        public bool IsPlural { get; set; } = false;
        public string? PluralValue { get; set; }
    }

    public class UserLanguagePreferenceDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string LanguageId { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = string.Empty;
        public string LanguageName { get; set; } = string.Empty;
        public string LanguageNativeName { get; set; } = string.Empty;
        public bool IsDefault { get; set; } = false;
        public string? DateFormat { get; set; }
        public string? TimeFormat { get; set; }
        public string? NumberFormat { get; set; }
        public string? Timezone { get; set; }
        public string? CurrencyCode { get; set; }
        public bool UseRTL { get; set; } = false;
        public bool HighContrast { get; set; } = false;
        public bool LargeText { get; set; } = false;
        public bool ReducedMotion { get; set; } = false;
        public bool ScreenReaderOptimized { get; set; } = false;
        public string? KeyboardNavigation { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpdateUserLanguagePreferenceRequest
    {
        public string? LanguageId { get; set; }
        public bool? IsDefault { get; set; }
        public string? DateFormat { get; set; }
        public string? TimeFormat { get; set; }
        public string? NumberFormat { get; set; }
        public string? Timezone { get; set; }
        public string? CurrencyCode { get; set; }
        public bool? UseRTL { get; set; }
        public bool? HighContrast { get; set; }
        public bool? LargeText { get; set; }
        public bool? ReducedMotion { get; set; }
        public bool? ScreenReaderOptimized { get; set; }
        public string? KeyboardNavigation { get; set; }
    }

    public class AccessibilityAuditDto
    {
        public string Id { get; set; } = string.Empty;
        public string PageUrl { get; set; } = string.Empty;
        public string PageTitle { get; set; } = string.Empty;
        public string AuditType { get; set; } = string.Empty;
        public string WCAGLevel { get; set; } = "AA";
        public DateTime AuditDate { get; set; }
        public string? AuditedBy { get; set; }
        public string? Tool { get; set; }
        public string? ToolVersion { get; set; }
        public int TotalIssues { get; set; } = 0;
        public int CriticalIssues { get; set; } = 0;
        public int SeriousIssues { get; set; } = 0;
        public int ModerateIssues { get; set; } = 0;
        public int MinorIssues { get; set; } = 0;
        public double ComplianceScore { get; set; } = 0.0;
        public string Status { get; set; } = "In Progress";
        public string? Summary { get; set; }
        public string? Recommendations { get; set; }
        public DateTime? NextAuditDate { get; set; }
        public string? Notes { get; set; }
        public bool IsPublic { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<AccessibilityIssueDto> Issues { get; set; } = new();
    }

    public class CreateAccessibilityAuditRequest
    {
        [Required]
        [MaxLength(200)]
        public string PageUrl { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string PageTitle { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string AuditType { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string WCAGLevel { get; set; } = "AA";
        
        [MaxLength(100)]
        public string? Tool { get; set; }
        
        [MaxLength(20)]
        public string? ToolVersion { get; set; }
        
        [MaxLength(5000)]
        public string? Summary { get; set; }
        
        [MaxLength(2000)]
        public string? Recommendations { get; set; }
        
        public DateTime? NextAuditDate { get; set; }
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        public bool IsPublic { get; set; } = false;
    }

    public class AccessibilityIssueDto
    {
        public string Id { get; set; } = string.Empty;
        public string AccessibilityAuditId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string IssueType { get; set; } = string.Empty;
        public string WCAGCriterion { get; set; } = string.Empty;
        public string? WCAGCriterionName { get; set; }
        public string WCAGLevel { get; set; } = "AA";
        public string? Element { get; set; }
        public string? ElementContext { get; set; }
        public string? PageLocation { get; set; }
        public string? CurrentValue { get; set; }
        public string? ExpectedValue { get; set; }
        public string? HowToFix { get; set; }
        public string? CodeExample { get; set; }
        public string Status { get; set; } = "Open";
        public string Priority { get; set; } = "Medium";
        public string? AssignedTo { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? FixedAt { get; set; }
        public string? FixedBy { get; set; }
        public string? FixNotes { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? VerifiedBy { get; set; }
        public string? TestingNotes { get; set; }
        public bool RequiresUserTesting { get; set; } = false;
        public bool RequiresScreenReaderTesting { get; set; } = false;
        public string? UserImpact { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateAccessibilityIssueRequest
    {
        [Required]
        public string AccessibilityAuditId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Severity { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string IssueType { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string WCAGCriterion { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? WCAGCriterionName { get; set; }
        
        [MaxLength(20)]
        public string WCAGLevel { get; set; } = "AA";
        
        [MaxLength(500)]
        public string? Element { get; set; }
        
        [MaxLength(1000)]
        public string? ElementContext { get; set; }
        
        [MaxLength(200)]
        public string? PageLocation { get; set; }
        
        [MaxLength(2000)]
        public string? CurrentValue { get; set; }
        
        [MaxLength(2000)]
        public string? ExpectedValue { get; set; }
        
        [MaxLength(2000)]
        public string? HowToFix { get; set; }
        
        [MaxLength(1000)]
        public string? CodeExample { get; set; }
        
        [MaxLength(50)]
        public string Priority { get; set; } = "Medium";
        
        [MaxLength(100)]
        public string? AssignedTo { get; set; }
        
        public bool RequiresUserTesting { get; set; } = false;
        public bool RequiresScreenReaderTesting { get; set; } = false;
        
        [MaxLength(2000)]
        public string? UserImpact { get; set; }
    }

    public class TranslationStatsDto
    {
        public string LanguageCode { get; set; } = string.Empty;
        public string LanguageName { get; set; } = string.Empty;
        public int TotalKeys { get; set; } = 0;
        public int TranslatedKeys { get; set; } = 0;
        public int ApprovedKeys { get; set; } = 0;
        public double CompletionPercentage { get; set; } = 0.0;
        public double ApprovalPercentage { get; set; } = 0.0;
        public DateTime? LastUpdated { get; set; }
        public List<CategoryStats> CategoryStats { get; set; } = new();
    }

    public class CategoryStats
    {
        public string Category { get; set; } = string.Empty;
        public int TotalKeys { get; set; } = 0;
        public int TranslatedKeys { get; set; } = 0;
        public int ApprovedKeys { get; set; } = 0;
        public double CompletionPercentage { get; set; } = 0.0;
    }
}
