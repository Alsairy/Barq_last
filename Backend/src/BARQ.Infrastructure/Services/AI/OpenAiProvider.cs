using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BARQ.Core.Interfaces;
using BARQ.Core.DTOs.AI;
using System.Text.Json;

namespace BARQ.Infrastructure.Services.AI;

public class OpenAiProvider : IAiProvider
{
    private readonly OpenAiConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenAiProvider> _logger;

    public OpenAiProvider(IOptions<OpenAiConfig> config, IHttpClientFactory httpClientFactory, ILogger<OpenAiProvider> logger)
    {
        _config = config.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string Name => "OpenAI";

    public async Task<AiResponse> ProcessRequestAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");

            var requestBody = new
            {
                model = _config.Model,
                messages = new[]
                {
                    new { role = "user", content = request.Prompt }
                },
                max_tokens = request.MaxTokens ?? _config.MaxTokens,
                temperature = request.Temperature ?? _config.Temperature
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{_config.BaseUrl}/v1/chat/completions", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);

            return new AiResponse
            {
                Content = responseData.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "",
                TokensUsed = responseData.GetProperty("usage").GetProperty("total_tokens").GetInt32(),
                Model = _config.Model,
                Provider = Name
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AI request");
            throw;
        }
    }

    public Task<bool> ValidateConfigurationAsync()
    {
        var isValid = !string.IsNullOrEmpty(_config.ApiKey) && 
                     !string.IsNullOrEmpty(_config.Model) && 
                     !string.IsNullOrEmpty(_config.BaseUrl);
        return Task.FromResult(isValid);
    }

    public Task<decimal> CalculateCostAsync(int inputTokens, int outputTokens)
    {
        var inputCostPer1K = 0.03m;
        var outputCostPer1K = 0.06m;
        
        var totalCost = (inputTokens / 1000m * inputCostPer1K) + (outputTokens / 1000m * outputCostPer1K);
        return Task.FromResult(totalCost);
    }
}

public class OpenAiConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com";
    public string Model { get; set; } = "gpt-4";
    public int MaxTokens { get; set; } = 1000;
    public float Temperature { get; set; } = 0.7f;
}
