using System.ComponentModel.DataAnnotations;
using BARQ.Core.Entities;

namespace BARQ.Core.DTOs
{
    public class AdminConfigurationDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string ConfigurationKey { get; set; } = string.Empty;
        public string ConfigurationValue { get; set; } = string.Empty;
        public AdminConfigurationType ConfigurationType { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public string? ValidatedBy { get; set; }
        public DateTime? ValidatedAt { get; set; }
        public string? Category { get; set; }
        public int Priority { get; set; }
        public string? Tags { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateAdminConfigurationRequest
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
        
        [MaxLength(100)]
        public string? Category { get; set; }
        
        [MaxLength(2000)]
        public string? ValidationRules { get; set; }
        
        public int Priority { get; set; } = 0;
        
        [MaxLength(1000)]
        public string? Tags { get; set; }
    }

    public class UpdateAdminConfigurationRequest
    {
        [Required]
        public string ConfigurationValue { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        public bool IsActive { get; set; }
        
        [MaxLength(100)]
        public string? Category { get; set; }
        
        [MaxLength(2000)]
        public string? ValidationRules { get; set; }
        
        public int Priority { get; set; }
        
        [MaxLength(1000)]
        public string? Tags { get; set; }
    }

    public class AdminConfigurationListRequest
    {
        public AdminConfigurationType? ConfigurationType { get; set; }
        public string? Category { get; set; }
        public bool? IsActive { get; set; }
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "ConfigurationKey";
        public string? SortDirection { get; set; } = "asc";
    }
}
