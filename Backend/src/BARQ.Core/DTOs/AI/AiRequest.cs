namespace BARQ.Core.DTOs.AI;

public class AiRequest
{
    public string Prompt { get; set; } = string.Empty;
    public int? MaxTokens { get; set; }
    public float? Temperature { get; set; }
    public string? Model { get; set; }
    public List<AiMessage>? Messages { get; set; }
}
