using BARQ.Core.DTOs.AI;
using BARQ.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BARQ.Infrastructure.Services.AI;

public class AiProviderFactory : IAiProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiProviderFactory> _logger;
    private readonly Dictionary<string, IAiProvider> _providers;

    public AiProviderFactory(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<AiProviderFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
        _providers = new Dictionary<string, IAiProvider>();
        
        InitializeProviders();
    }

    public IAiProvider CreateProvider(string providerType, Dictionary<string, string> configuration)
    {
        return providerType.ToLowerInvariant() switch
        {
            "openai" => _serviceProvider.GetRequiredService<OpenAiProvider>(),
            "azure-openai" => _serviceProvider.GetRequiredService<AzureOpenAiProvider>(),
            _ => throw new NotSupportedException($"Provider type '{providerType}' is not supported")
        };
    }

    public IEnumerable<IAiProvider> GetAvailableProviders()
    {
        return _providers.Values.Where(p => p.IsEnabled);
    }

    public IAiProvider GetProvider(string name)
    {
        if (_providers.TryGetValue(name, out var provider))
        {
            return provider;
        }
        
        throw new InvalidOperationException($"Provider '{name}' not found or not enabled");
    }

    private void InitializeProviders()
    {
        try
        {
            var openAiProvider = _serviceProvider.GetRequiredService<OpenAiProvider>();
            if (openAiProvider.IsEnabled)
            {
                _providers[openAiProvider.Name] = openAiProvider;
                _logger.LogInformation("Initialized OpenAI provider");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize OpenAI provider");
        }

        try
        {
            var azureProvider = _serviceProvider.GetRequiredService<AzureOpenAiProvider>();
            if (azureProvider.IsEnabled)
            {
                _providers[azureProvider.Name] = azureProvider;
                _logger.LogInformation("Initialized Azure OpenAI provider");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize Azure OpenAI provider");
        }

        _logger.LogInformation("Initialized {Count} AI providers", _providers.Count);
    }
}
