using BARQ.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BARQ.Application.Services.Workflow
{
    public class SlaMonitorWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SlaMonitorWorker> _logger;

        public SlaMonitorWorker(IServiceProvider serviceProvider, ILogger<SlaMonitorWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var slaService = scope.ServiceProvider.GetRequiredService<ISlaService>();
                    var escalationService = scope.ServiceProvider.GetRequiredService<IEscalationService>();

                    await MonitorSlaViolationsAsync(slaService, escalationService);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in SLA monitoring worker");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task MonitorSlaViolationsAsync(ISlaService slaService, IEscalationService escalationService)
        {
            _logger.LogInformation("Monitoring SLA violations at {Time}", DateTime.UtcNow);
            await System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
