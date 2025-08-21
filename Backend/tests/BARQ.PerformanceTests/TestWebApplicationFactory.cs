using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BARQ.Infrastructure.Data;
using BARQ.Core.Services;
using BARQ.PerformanceTests.Mocks;

namespace BARQ.PerformanceTests;

public class TestWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<BarqDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            var tenantProviderDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ITenantProvider));
            if (tenantProviderDescriptor != null)
            {
                services.Remove(tenantProviderDescriptor);
            }

            services.AddDbContext<BarqDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            services.AddScoped<ITenantProvider, MockTenantProvider>();

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<BarqDbContext>();

            db.Database.EnsureCreated();
        });
    }
}
