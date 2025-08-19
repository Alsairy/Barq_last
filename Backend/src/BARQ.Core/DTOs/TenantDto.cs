using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.DTOs
{
    public class TenantDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public string? SubscriptionTier { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateTenantRequest
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        public string DisplayName { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [EmailAddress]
        [MaxLength(500)]
        public string? ContactEmail { get; set; }
        
        [MaxLength(100)]
        public string? ContactPhone { get; set; }
        
        [MaxLength(1000)]
        public string? Address { get; set; }
        
        [MaxLength(100)]
        public string? SubscriptionTier { get; set; }
    }

    public class UpdateTenantRequest
    {
        [Required]
        [MaxLength(255)]
        public string DisplayName { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [EmailAddress]
        [MaxLength(500)]
        public string? ContactEmail { get; set; }
        
        [MaxLength(100)]
        public string? ContactPhone { get; set; }
        
        [MaxLength(1000)]
        public string? Address { get; set; }
        
        public bool IsActive { get; set; }
        
        [MaxLength(100)]
        public string? SubscriptionTier { get; set; }
    }
}
