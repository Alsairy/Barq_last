namespace BARQ.Core.DTOs.AI;

public class AiResponse
{
    public string Content { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public string Model { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}
