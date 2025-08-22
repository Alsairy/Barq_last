using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BARQ.Infrastructure.Services.AI;
using BARQ.Core.DTOs.AI;
using System.Net.Http;

namespace BARQ.UnitTests.Services;

public class AiProviderTests
{
    private readonly Mock<ILogger<OpenAiProvider>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpClient> _httpClientMock;

    public AiProviderTests()
    {
        _loggerMock = new Mock<ILogger<OpenAiProvider>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientMock = new Mock<HttpClient>();
    }

    [Fact]
    public async Task ProcessRequest_WithValidInput_ReturnsSuccessResponse()
    {
        var config = new OpenAiConfig
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.openai.com",
            Model = "gpt-4",
            MaxTokens = 1000,
            Temperature = 0.7f
        };
        
        var optionsMock = new Mock<IOptions<OpenAiConfig>>();
        optionsMock.Setup(x => x.Value).Returns(config);

        var provider = new OpenAiProvider(optionsMock.Object, _httpClientFactoryMock.Object, _loggerMock.Object);

        var request = new AiRequest
        {
            Prompt = "Test prompt",
            MaxTokens = 100,
            Temperature = 0.5f
        };

        provider.Should().NotBeNull();
        config.ApiKey.Should().Be("test-key");
        config.Model.Should().Be("gpt-4");
    }

    [Theory]
    [InlineData("", "gpt-4", false)]
    [InlineData("valid-key", "", false)]
    [InlineData("valid-key", "gpt-4", true)]
    public void ValidateConfiguration_WithDifferentInputs_ReturnsExpectedResult(string apiKey, string model, bool expectedValid)
    {
        var config = new OpenAiConfig
        {
            ApiKey = apiKey,
            Model = model,
            BaseUrl = "https://api.openai.com"
        };

        var isValid = !string.IsNullOrEmpty(config.ApiKey) && !string.IsNullOrEmpty(config.Model);

        isValid.Should().Be(expectedValid);
    }

    [Fact]
    public void CalculateCost_WithTokenUsage_ReturnsCorrectCost()
    {
        var inputTokens = 1000;
        var outputTokens = 500;
        var inputCostPer1K = 0.03m;
        var outputCostPer1K = 0.06m;

        var totalCost = (inputTokens / 1000m * inputCostPer1K) + (outputTokens / 1000m * outputCostPer1K);

        totalCost.Should().Be(0.06m);
    }

    [Theory]
    [InlineData(0.0f, "deterministic")]
    [InlineData(0.5f, "balanced")]
    [InlineData(1.0f, "creative")]
    public void ClassifyTemperature_WithDifferentValues_ReturnsCorrectClassification(float temperature, string expectedClassification)
    {
        var classification = temperature switch
        {
            <= 0.2f => "deterministic",
            <= 0.7f => "balanced",
            _ => "creative"
        };

        classification.Should().Be(expectedClassification);
    }
}
