using BARQ.Core.DTOs.AI;
using BARQ.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace BARQ.Infrastructure.Services.AI;

public class AzureOpenAiProvider : IAiProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureOpenAiProvider> _logger;
    private readonly string _apiKey;
    private readonly string _endpoint;
    private readonly string _apiVersion;
    private readonly Dictionary<string, decimal> _modelCosts;

    public string Name => "Azure OpenAI";
    public string Type => "azure-openai";
    public bool IsEnabled { get; private set; }

    public AzureOpenAiProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<AzureOpenAiProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        
        _apiKey = configuration["AI:AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("Azure OpenAI API key not configured");
        _endpoint = configuration["AI:AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("Azure OpenAI endpoint not configured");
        _apiVersion = configuration["AI:AzureOpenAI:ApiVersion"] ?? "2023-12-01-preview";
        IsEnabled = !string.IsNullOrEmpty(_apiKey) && !string.IsNullOrEmpty(_endpoint);
        
        _modelCosts = new Dictionary<string, decimal>
        {
            { "gpt-4", 0.03m },
            { "gpt-4-32k", 0.06m },
            { "gpt-35-turbo", 0.002m },
            { "gpt-35-turbo-16k", 0.004m }
        };
    }

    public async Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var requestId = Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation("Starting Azure OpenAI request {RequestId} for model {Model}", requestId, request.Model);
            
            var httpClient = _httpClientFactory.CreateClient("AzureOpenAI");
            httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
            httpClient.Timeout = TimeSpan.FromMinutes(2);

            var deploymentName = GetDeploymentName(request.Model);
            var url = $"{_endpoint}/openai/deployments/{deploymentName}/chat/completions?api-version={_apiVersion}";

            var payload = CreateAzureOpenAiPayload(request);
            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var azureResponse = JsonSerializer.Deserialize<AzureOpenAiChatResponse>(responseContent);

            if (azureResponse == null)
                throw new InvalidOperationException("Failed to deserialize Azure OpenAI response");

            var duration = DateTime.UtcNow - startTime;
            var cost = CalculateCost(request.Model, azureResponse.Usage.TotalTokens);

            var aiResponse = new AiResponse
            {
                Content = azureResponse.Choices.FirstOrDefault()?.Message.Content ?? string.Empty,
                Model = request.Model,
                Usage = new AiUsage
                {
                    PromptTokens = azureResponse.Usage.PromptTokens,
                    CompletionTokens = azureResponse.Usage.CompletionTokens,
                    TotalTokens = azureResponse.Usage.TotalTokens,
                    Cost = cost,
                    Currency = "USD"
                },
                FinishReason = azureResponse.Choices.FirstOrDefault()?.FinishReason,
                Choices = azureResponse.Choices.Select(c => new AiChoice
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

            _logger.LogInformation("Completed Azure OpenAI request {RequestId} in {Duration}ms, cost: ${Cost}", 
                requestId, duration.TotalMilliseconds, cost);

            return aiResponse;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Azure OpenAI request {RequestId} failed after {Duration}ms", requestId, duration.TotalMilliseconds);
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
            var httpClient = _httpClientFactory.CreateClient("AzureOpenAI");
            httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var url = $"{_endpoint}/openai/deployments?api-version={_apiVersion}";
            var response = await httpClient.GetAsync(url, cancellationToken);
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
                    { "Endpoint", _endpoint },
                    { "ApiVersion", _apiVersion }
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

    private object CreateAzureOpenAiPayload(AiRequest request)
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
            messages = messages,
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
            stop = request.Stop
        };
    }

    private string GetDeploymentName(string model)
    {
        return _configuration[$"AI:AzureOpenAI:Deployments:{model}"] ?? model;
    }

    private decimal CalculateCost(string model, int totalTokens)
    {
        if (_modelCosts.TryGetValue(model, out var costPerToken))
        {
            return (decimal)totalTokens * costPerToken / 1000m;
        }
        
        return 0m;
    }

    private class AzureOpenAiChatResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Object { get; set; } = string.Empty;
        public long Created { get; set; }
        public string Model { get; set; } = string.Empty;
        public List<AzureOpenAiChoice> Choices { get; set; } = new();
        public AzureOpenAiUsage Usage { get; set; } = new();
    }

    private class AzureOpenAiChoice
    {
        public int Index { get; set; }
        public AzureOpenAiMessage Message { get; set; } = new();
        public string? FinishReason { get; set; }
    }

    private class AzureOpenAiMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private class AzureOpenAiUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
