using BARQ.Core.DTOs.AI;

namespace BARQ.Core.Interfaces;

public interface IAiProvider
{
    string Name { get; }
    string Type { get; }
    bool IsEnabled { get; }
    
    Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default);
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);
    Task<AiProviderHealth> GetHealthAsync(CancellationToken cancellationToken = default);
}

public interface IAiProviderFactory
{
    IAiProvider CreateProvider(string providerType, Dictionary<string, string> configuration);
    IEnumerable<IAiProvider> GetAvailableProviders();
    IAiProvider GetProvider(string name);
}

public interface IAiProviderSelector
{
    Task<IAiProvider> SelectProviderAsync(AiRequest request, CancellationToken cancellationToken = default);
    void UpdateProviderMetrics(string providerName, AiProviderMetrics metrics);
}
