using System.ComponentModel.DataAnnotations;

namespace BARQ.Core.DTOs
{
    public class AIProviderDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ProviderType { get; set; } = string.Empty;
        public string ApiEndpoint { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
        public int Priority { get; set; }
        public int MaxConcurrentRequests { get; set; }
        public int TimeoutSeconds { get; set; }
        public string? Version { get; set; }
        public DateTime? LastHealthCheck { get; set; }
        public bool IsHealthy { get; set; }
        public string? HealthCheckMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<AIAgentDto> AIAgents { get; set; } = new();
    }

    public class CreateAIProviderRequest
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string ProviderType { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string ApiEndpoint { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? ApiKey { get; set; }
        
        [MaxLength(2000)]
        public string? Configuration { get; set; }
        
        public bool IsDefault { get; set; } = false;
        public int Priority { get; set; } = 0;
        public int MaxConcurrentRequests { get; set; } = 10;
        public int TimeoutSeconds { get; set; } = 300;
        
        [MaxLength(100)]
        public string? Version { get; set; }
    }

    public class UpdateAIProviderRequest
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(1000)]
        public string ApiEndpoint { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? ApiKey { get; set; }
        
        [MaxLength(2000)]
        public string? Configuration { get; set; }
        
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
        public int Priority { get; set; }
        public int MaxConcurrentRequests { get; set; }
        public int TimeoutSeconds { get; set; }
        
        [MaxLength(100)]
        public string? Version { get; set; }
    }

    public class AIAgentDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid ProviderId { get; set; }
        public string? ProviderName { get; set; }
        public string AgentType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
        public int Priority { get; set; }
        public decimal? CostPerRequest { get; set; }
        public string? Model { get; set; }
        public int MaxTokens { get; set; }
        public decimal Temperature { get; set; }
        public string? Capabilities { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateAIAgentRequest
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        public Guid ProviderId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string AgentType { get; set; } = string.Empty;
        
        [MaxLength(2000)]
        public string? Configuration { get; set; }
        
        [MaxLength(5000)]
        public string? SystemPrompt { get; set; }
        
        public bool IsDefault { get; set; } = false;
        public int Priority { get; set; } = 0;
        public decimal? CostPerRequest { get; set; }
        
        [MaxLength(100)]
        public string? Model { get; set; }
        
        public int MaxTokens { get; set; } = 4000;
        public decimal Temperature { get; set; } = 0.7m;
        
        [MaxLength(1000)]
        public string? Capabilities { get; set; }
    }
}
