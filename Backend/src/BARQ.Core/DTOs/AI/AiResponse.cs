namespace BARQ.Core.DTOs.AI;

public class AiResponse
{
    public string Content { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public AiUsage Usage { get; set; } = new();
    public string? FinishReason { get; set; }
    public List<AiChoice> Choices { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }
    public string RequestId { get; set; } = string.Empty;
}

public class AiUsage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public decimal Cost { get; set; }
    public string Currency { get; set; } = "USD";
}

public class AiChoice
{
    public int Index { get; set; }
    public AiMessage Message { get; set; } = new();
    public string? FinishReason { get; set; }
}

public class AiProviderHealth
{
    public string ProviderName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Details { get; set; } = new();
}

public class AiProviderMetrics
{
    public string ProviderName { get; set; } = string.Empty;
    public TimeSpan AverageLatency { get; set; }
    public decimal AverageCost { get; set; }
    public double SuccessRate { get; set; }
    public double QualityScore { get; set; }
    public int RequestCount { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
