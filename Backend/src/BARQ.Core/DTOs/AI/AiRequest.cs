namespace BARQ.Core.DTOs.AI;

public class AiRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 1000;
    public string[]? Stop { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? SystemMessage { get; set; }
    public List<AiMessage> Messages { get; set; } = new();
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class AiMessage
{
    public string Role { get; set; } = string.Empty; // "system", "user", "assistant"
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}
