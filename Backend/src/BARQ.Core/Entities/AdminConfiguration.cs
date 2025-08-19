using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BARQ.Core.Entities
{
    [Table("AdminConfigurations")]
    public class AdminConfiguration : BaseEntity
    {
        [Required]
        [MaxLength(255)]
        public string ConfigurationKey { get; set; } = string.Empty;
        
        [Required]
        public string ConfigurationValue { get; set; } = string.Empty;
        
        [Required]
        public AdminConfigurationType ConfigurationType { get; set; }
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        [MaxLength(255)]
        public string? ValidatedBy { get; set; }
        
        public DateTime? ValidatedAt { get; set; }
        
        [MaxLength(100)]
        public string? Category { get; set; }
        
        [MaxLength(2000)]
        public string? ValidationRules { get; set; }
        
        public int Priority { get; set; } = 0;
        
        [MaxLength(1000)]
        public string? Tags { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;
        
        [ForeignKey("ValidatedBy")]
        public virtual ApplicationUser? ValidatedByUser { get; set; }
        
        public virtual ICollection<AdminConfigurationHistory> ConfigurationHistory { get; set; } = new List<AdminConfigurationHistory>();
    }

    public enum AdminConfigurationType
    {
        TechnologyConstraint,
        TemplateConfiguration,
        WorkflowConfiguration,
        SecurityConfiguration,
        IntegrationConfiguration,
        UIConfiguration,
        BusinessRuleConfiguration
    }
}
