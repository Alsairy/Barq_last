using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BARQ.Application.Services.Workflow;
using BARQ.Core.Services;

namespace BARQ.IntegrationTests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.Configure<FlowableOptions>(options =>
            {
                options.BaseUrl = "http://localhost:8080"; // Assumes Flowable running locally for tests
                options.ServiceAccountToken = "test-token";
                options.TimeoutSeconds = 30;
                options.RetryCount = 2; // Reduced for faster tests
                options.RetryDelaySeconds = 1;
                options.CircuitBreakerFailureThreshold = 3;
                options.CircuitBreakerTimeoutSeconds = 10;
            });

            services.AddScoped<ITenantProvider, MockTenantProvider>();
        });

        builder.UseEnvironment("Testing");
    }
}
