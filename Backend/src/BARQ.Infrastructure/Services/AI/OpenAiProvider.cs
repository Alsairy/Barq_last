using BARQ.Core.DTOs.AI;
using BARQ.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace BARQ.Infrastructure.Services.AI;

public class OpenAiProvider : IAiProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiProvider> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly Dictionary<string, decimal> _modelCosts;

    public string Name => "OpenAI";
    public string Type => "openai";
    public bool IsEnabled { get; private set; }

    public OpenAiProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<OpenAiProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        
        _apiKey = configuration["AI:OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not configured");
        _baseUrl = configuration["AI:OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";
        IsEnabled = !string.IsNullOrEmpty(_apiKey);
        
        _modelCosts = new Dictionary<string, decimal>
        {
            { "gpt-4", 0.03m },
            { "gpt-4-32k", 0.06m },
            { "gpt-3.5-turbo", 0.002m },
            { "gpt-3.5-turbo-16k", 0.004m },
            { "text-davinci-003", 0.02m },
            { "text-curie-001", 0.002m },
            { "text-babbage-001", 0.0005m },
            { "text-ada-001", 0.0004m }
        };
    }

    public async Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var requestId = Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation("Starting OpenAI request {RequestId} for model {Model}", requestId, request.Model);
            
            var httpClient = _httpClientFactory.CreateClient("OpenAI");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            httpClient.Timeout = TimeSpan.FromMinutes(2);

            var payload = CreateOpenAiPayload(request);
            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{_baseUrl}/chat/completions", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var openAiResponse = JsonSerializer.Deserialize<OpenAiChatResponse>(responseContent);

            if (openAiResponse == null)
                throw new InvalidOperationException("Failed to deserialize OpenAI response");

            var duration = DateTime.UtcNow - startTime;
            var cost = CalculateCost(request.Model, openAiResponse.Usage.TotalTokens);

            var aiResponse = new AiResponse
            {
                Content = openAiResponse.Choices.FirstOrDefault()?.Message.Content ?? string.Empty,
                Model = request.Model,
                Usage = new AiUsage
                {
                    PromptTokens = openAiResponse.Usage.PromptTokens,
                    CompletionTokens = openAiResponse.Usage.CompletionTokens,
                    TotalTokens = openAiResponse.Usage.TotalTokens,
                    Cost = cost,
                    Currency = "USD"
                },
                FinishReason = openAiResponse.Choices.FirstOrDefault()?.FinishReason,
                Choices = openAiResponse.Choices.Select(c => new AiChoice
                {
                    Index = c.Index,
                    Message = new AiMessage
                    {
                        Role = c.Message.Role,
                        Content = c.Message.Content
                    },
                    FinishReason = c.FinishReason
                }).ToList(),
                Duration = duration,
                RequestId = requestId,
                CreatedAt = startTime
            };

            _logger.LogInformation("Completed OpenAI request {RequestId} in {Duration}ms, cost: ${Cost}", 
                requestId, duration.TotalMilliseconds, cost);

            return aiResponse;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "OpenAI request {RequestId} failed after {Duration}ms", requestId, duration.TotalMilliseconds);
            throw;
        }
    }

    public async Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var health = await GetHealthAsync(cancellationToken);
            return health.IsHealthy;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AiProviderHealth> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var httpClient = _httpClientFactory.CreateClient("OpenAI");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var response = await httpClient.GetAsync($"{_baseUrl}/models", cancellationToken);
            var responseTime = DateTime.UtcNow - startTime;

            return new AiProviderHealth
            {
                ProviderName = Name,
                IsHealthy = response.IsSuccessStatusCode,
                Status = response.IsSuccessStatusCode ? "Healthy" : $"Error: {response.StatusCode}",
                ResponseTime = responseTime,
                LastChecked = DateTime.UtcNow,
                Details = new Dictionary<string, object>
                {
                    { "StatusCode", (int)response.StatusCode },
                    { "BaseUrl", _baseUrl }
                }
            };
        }
        catch (Exception ex)
        {
            var responseTime = DateTime.UtcNow - startTime;
            return new AiProviderHealth
            {
                ProviderName = Name,
                IsHealthy = false,
                Status = "Error",
                ResponseTime = responseTime,
                ErrorMessage = ex.Message,
                LastChecked = DateTime.UtcNow
            };
        }
    }

    private object CreateOpenAiPayload(AiRequest request)
    {
        var messages = new List<object>();
        
        if (!string.IsNullOrEmpty(request.SystemMessage))
        {
            messages.Add(new { role = "system", content = request.SystemMessage });
        }

        if (request.Messages.Any())
        {
            messages.AddRange(request.Messages.Select(m => new { role = m.Role, content = m.Content }));
        }
        else
        {
            messages.Add(new { role = "user", content = request.Prompt });
        }

        return new
        {
            model = request.Model,
            messages = messages,
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
            stop = request.Stop
        };
    }

    private decimal CalculateCost(string model, int totalTokens)
    {
        if (_modelCosts.TryGetValue(model, out var costPerToken))
        {
            return (decimal)totalTokens * costPerToken / 1000m;
        }
        
        return 0m;
    }

    private class OpenAiChatResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Object { get; set; } = string.Empty;
        public long Created { get; set; }
        public string Model { get; set; } = string.Empty;
        public List<OpenAiChoice> Choices { get; set; } = new();
        public OpenAiUsage Usage { get; set; } = new();
    }

    private class OpenAiChoice
    {
        public int Index { get; set; }
        public OpenAiMessage Message { get; set; } = new();
        public string? FinishReason { get; set; }
    }

    private class OpenAiMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private class OpenAiUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
