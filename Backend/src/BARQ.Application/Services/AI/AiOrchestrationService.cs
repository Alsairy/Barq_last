using BARQ.Core.DTOs.AI;
using BARQ.Core.Interfaces;
using BARQ.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BARQ.Application.Services.AI;

public interface IAiOrchestrationService
{
    Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default);
    Task<AiProviderHealth[]> GetProviderHealthAsync(CancellationToken cancellationToken = default);
    Task<AiProviderMetrics[]> GetProviderMetricsAsync(CancellationToken cancellationToken = default);
}

public class AiOrchestrationService : IAiOrchestrationService
{
    private readonly IAiProviderSelector _providerSelector;
    private readonly IAiProviderFactory _providerFactory;
    private readonly BarqDbContext _context;
    private readonly ILogger<AiOrchestrationService> _logger;

    public AiOrchestrationService(
        IAiProviderSelector providerSelector,
        IAiProviderFactory providerFactory,
        BarqDbContext context,
        ILogger<AiOrchestrationService> logger)
    {
        _providerSelector = providerSelector;
        _providerFactory = providerFactory;
        _context = context;
        _logger = logger;
    }

    public async Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation("Starting AI generation request {RequestId} for model {Model}", 
                requestId, request.Model);

            var provider = await _providerSelector.SelectProviderAsync(request, cancellationToken);
            
            var response = await provider.GenerateAsync(request, cancellationToken);
            stopwatch.Stop();

            var metrics = new AiProviderMetrics
            {
                ProviderName = provider.Name,
                AverageLatency = stopwatch.Elapsed,
                AverageCost = response.Usage.Cost,
                SuccessRate = 1.0, // Success
                QualityScore = CalculateQualityScore(response),
                RequestCount = 1,
                LastUpdated = DateTime.UtcNow
            };

            _providerSelector.UpdateProviderMetrics(provider.Name, metrics);

            await LogTelemetryAsync(provider.Name, request, response, stopwatch.Elapsed, true, null);

            _logger.LogInformation("Completed AI generation request {RequestId} using {ProviderName} in {Duration}ms", 
                requestId, provider.Name, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, "AI generation request {RequestId} failed after {Duration}ms", 
                requestId, stopwatch.ElapsedMilliseconds);

            try
            {
                var provider = await _providerSelector.SelectProviderAsync(request, cancellationToken);
                var metrics = new AiProviderMetrics
                {
                    ProviderName = provider.Name,
                    AverageLatency = stopwatch.Elapsed,
                    AverageCost = 0,
                    SuccessRate = 0.0, // Failure
                    QualityScore = 0.0,
                    RequestCount = 1,
                    LastUpdated = DateTime.UtcNow
                };

                _providerSelector.UpdateProviderMetrics(provider.Name, metrics);
                await LogTelemetryAsync(provider.Name, request, null, stopwatch.Elapsed, false, ex.Message);
            }
            catch
            {
            }

            throw;
        }
    }

    public async Task<AiProviderHealth[]> GetProviderHealthAsync(CancellationToken cancellationToken = default)
    {
        var providers = _providerFactory.GetAvailableProviders();
        var healthTasks = providers.Select(p => p.GetHealthAsync(cancellationToken));
        
        return await Task.WhenAll(healthTasks);
    }

    public async Task<AiProviderMetrics[]> GetProviderMetricsAsync(CancellationToken cancellationToken = default)
    {
        return Array.Empty<AiProviderMetrics>();
    }

    private double CalculateQualityScore(AiResponse response)
    {
        var score = 1.0;
        
        if (response.Content.Length < 10)
            score -= 0.3;
        
        if (response.FinishReason == "length")
            score -= 0.2;
        
        if (response.FinishReason == "stop")
            score += 0.1;
        
        return Math.Max(0.0, Math.Min(1.0, score));
    }

    private async Task LogTelemetryAsync(string providerName, AiRequest request, AiResponse? response, 
        TimeSpan duration, bool success, string? errorMessage)
    {
        try
        {
            var telemetry = new
            {
                Timestamp = DateTime.UtcNow,
                ProviderName = providerName,
                Model = request.Model,
                Success = success,
                Duration = duration.TotalMilliseconds,
                TokensUsed = response?.Usage.TotalTokens ?? 0,
                Cost = response?.Usage.Cost ?? 0,
                ErrorMessage = errorMessage,
                UserId = request.UserId,
                SessionId = request.SessionId
            };

            _logger.LogInformation("AI Telemetry: {@Telemetry}", telemetry);

        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log AI telemetry");
        }
    }
}
