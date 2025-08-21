using BARQ.Core.DTOs.AI;
using BARQ.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BARQ.Infrastructure.Services.AI;

public class AiProviderSelector : IAiProviderSelector
{
    private readonly IAiProviderFactory _providerFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiProviderSelector> _logger;
    private readonly ConcurrentDictionary<string, AiProviderMetrics> _metrics;
    private readonly string _selectionStrategy;

    public AiProviderSelector(IAiProviderFactory providerFactory, IConfiguration configuration, ILogger<AiProviderSelector> logger)
    {
        _providerFactory = providerFactory;
        _configuration = configuration;
        _logger = logger;
        _metrics = new ConcurrentDictionary<string, AiProviderMetrics>();
        _selectionStrategy = configuration["AI:SelectionStrategy"] ?? "round-robin";
    }

    public async Task<IAiProvider> SelectProviderAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        var availableProviders = _providerFactory.GetAvailableProviders().ToList();
        
        if (!availableProviders.Any())
        {
            throw new InvalidOperationException("No AI providers are available");
        }

        if (availableProviders.Count == 1)
        {
            return availableProviders.First();
        }

        var selectedProvider = _selectionStrategy.ToLowerInvariant() switch
        {
            "round-robin" => SelectRoundRobin(availableProviders),
            "best-latency" => SelectBestLatency(availableProviders),
            "cost-bias" => SelectCostBias(availableProviders),
            "quality-score" => SelectBestQuality(availableProviders),
            _ => SelectRoundRobin(availableProviders)
        };

        _logger.LogDebug("Selected provider {ProviderName} using strategy {Strategy}", 
            selectedProvider.Name, _selectionStrategy);

        return selectedProvider;
    }

    public void UpdateProviderMetrics(string providerName, AiProviderMetrics metrics)
    {
        _metrics.AddOrUpdate(providerName, metrics, (key, existing) =>
        {
            var alpha = 0.3; // Weight for new values
            
            return new AiProviderMetrics
            {
                ProviderName = providerName,
                AverageLatency = TimeSpan.FromMilliseconds(
                    alpha * metrics.AverageLatency.TotalMilliseconds + 
                    (1 - alpha) * existing.AverageLatency.TotalMilliseconds),
                AverageCost = (decimal)(alpha * (double)metrics.AverageCost + 
                    (1 - alpha) * (double)existing.AverageCost),
                SuccessRate = alpha * metrics.SuccessRate + (1 - alpha) * existing.SuccessRate,
                QualityScore = alpha * metrics.QualityScore + (1 - alpha) * existing.QualityScore,
                RequestCount = existing.RequestCount + 1,
                LastUpdated = DateTime.UtcNow
            };
        });

        _logger.LogDebug("Updated metrics for provider {ProviderName}: Latency={Latency}ms, Cost=${Cost}, Success={Success}%, Quality={Quality}", 
            providerName, metrics.AverageLatency.TotalMilliseconds, metrics.AverageCost, 
            metrics.SuccessRate * 100, metrics.QualityScore);
    }

    private IAiProvider SelectRoundRobin(List<IAiProvider> providers)
    {
        var index = Environment.TickCount % providers.Count;
        return providers[index];
    }

    private IAiProvider SelectBestLatency(List<IAiProvider> providers)
    {
        var bestProvider = providers.First();
        var bestLatency = TimeSpan.MaxValue;

        foreach (var provider in providers)
        {
            if (_metrics.TryGetValue(provider.Name, out var metrics))
            {
                if (metrics.AverageLatency < bestLatency)
                {
                    bestLatency = metrics.AverageLatency;
                    bestProvider = provider;
                }
            }
        }

        return bestProvider;
    }

    private IAiProvider SelectCostBias(List<IAiProvider> providers)
    {
        var bestProvider = providers.First();
        var bestCost = decimal.MaxValue;

        foreach (var provider in providers)
        {
            if (_metrics.TryGetValue(provider.Name, out var metrics))
            {
                if (metrics.AverageCost < bestCost)
                {
                    bestCost = metrics.AverageCost;
                    bestProvider = provider;
                }
            }
        }

        return bestProvider;
    }

    private IAiProvider SelectBestQuality(List<IAiProvider> providers)
    {
        var bestProvider = providers.First();
        var bestQuality = 0.0;

        foreach (var provider in providers)
        {
            if (_metrics.TryGetValue(provider.Name, out var metrics))
            {
                if (metrics.QualityScore > bestQuality)
                {
                    bestQuality = metrics.QualityScore;
                    bestProvider = provider;
                }
            }
        }

        return bestProvider;
    }
}
