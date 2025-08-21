using BARQ.Application.Services.AI;
using BARQ.Core.DTOs.AI;
using BARQ.Core.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BARQ.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiOrchestrationService _aiOrchestrationService;
    private readonly ILogger<AiController> _logger;

    public AiController(IAiOrchestrationService aiOrchestrationService, ILogger<AiController> logger)
    {
        _aiOrchestrationService = aiOrchestrationService;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<ApiResponse<AiResponse>>> GenerateAsync([FromBody] AiRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Prompt) && !request.Messages.Any())
            {
                return BadRequest(ApiResponse<AiResponse>.Fail("Either prompt or messages must be provided"));
            }

            if (string.IsNullOrEmpty(request.Model))
            {
                return BadRequest(ApiResponse<AiResponse>.Fail("Model must be specified"));
            }

            var response = await _aiOrchestrationService.GenerateAsync(request, cancellationToken);
            
            return Ok(ApiResponse<AiResponse>.Ok(response));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid AI generation request");
            return BadRequest(ApiResponse<AiResponse>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI response");
            return StatusCode(500, ApiResponse<AiResponse>.Fail("An error occurred while generating the AI response"));
        }
    }

    [HttpGet("health")]
    public async Task<ActionResult<ApiResponse<AiProviderHealth[]>>> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var health = await _aiOrchestrationService.GetProviderHealthAsync(cancellationToken);
            return Ok(ApiResponse<AiProviderHealth[]>.Ok(health));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI provider health");
            return StatusCode(500, ApiResponse<AiProviderHealth[]>.Fail("An error occurred while checking provider health"));
        }
    }

    [HttpGet("metrics")]
    [Authorize(Policy = "RequireAdministratorRole")]
    public async Task<ActionResult<ApiResponse<AiProviderMetrics[]>>> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await _aiOrchestrationService.GetProviderMetricsAsync(cancellationToken);
            return Ok(ApiResponse<AiProviderMetrics[]>.Ok(metrics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI provider metrics");
            return StatusCode(500, ApiResponse<AiProviderMetrics[]>.Fail("An error occurred while retrieving provider metrics"));
        }
    }

    [HttpPost("chat")]
    public async Task<ActionResult<ApiResponse<AiResponse>>> ChatAsync([FromBody] AiChatRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!request.Messages.Any())
            {
                return BadRequest(ApiResponse<AiResponse>.Fail("Messages must be provided for chat"));
            }

            var aiRequest = new AiRequest
            {
                Messages = request.Messages,
                Model = request.Model ?? "gpt-3.5-turbo",
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                SystemMessage = request.SystemMessage,
                UserId = User.Identity?.Name,
                SessionId = request.SessionId,
                Metadata = request.Metadata
            };

            var response = await _aiOrchestrationService.GenerateAsync(aiRequest, cancellationToken);
            
            return Ok(ApiResponse<AiResponse>.Ok(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AI chat");
            return StatusCode(500, ApiResponse<AiResponse>.Fail("An error occurred during the chat"));
        }
    }
}

public class AiChatRequest
{
    public List<AiMessage> Messages { get; set; } = new();
    public string? Model { get; set; }
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 1000;
    public string? SystemMessage { get; set; }
    public string? SessionId { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
