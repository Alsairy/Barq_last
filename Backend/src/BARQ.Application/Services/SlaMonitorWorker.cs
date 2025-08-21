using BARQ.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BARQ.Application.Services;

public class SlaMonitorWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SlaMonitorWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public SlaMonitorWorker(IServiceProvider serviceProvider, ILogger<SlaMonitorWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SLA Monitor Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSlaMonitoringAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during SLA monitoring");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("SLA Monitor Worker stopped");
    }

    private async Task ProcessSlaMonitoringAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var slaService = scope.ServiceProvider.GetRequiredService<ISlaService>();
        var escalationService = scope.ServiceProvider.GetRequiredService<IEscalationService>();

        try
        {
            _logger.LogDebug("Starting SLA violation check");
            await slaService.CheckAndCreateViolationsAsync(cancellationToken);

            _logger.LogDebug("Starting escalation processing");
            await escalationService.ProcessEscalationsAsync(cancellationToken);

            _logger.LogDebug("SLA monitoring cycle completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SLA monitoring cycle");
            throw;
        }
    }
}
