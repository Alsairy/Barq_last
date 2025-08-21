using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BARQ.Infrastructure.Data;
using BARQ.Core.Services;
using BARQ.IntegrationTests.Mocks;

namespace BARQ.IntegrationTests;

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
            var userManager = scopedServices.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<BARQ.Core.Entities.ApplicationUser>>();
            var roleManager = scopedServices.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole<Guid>>>();

            db.Database.EnsureCreated();
            
            SeedTestData(db, userManager, roleManager).Wait();
        });

        builder.UseEnvironment("Development");
    }

    private static async System.Threading.Tasks.Task SeedTestData(BarqDbContext context, Microsoft.AspNetCore.Identity.UserManager<BARQ.Core.Entities.ApplicationUser> userManager, Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole<Guid>> roleManager)
    {
        if (!await roleManager.RoleExistsAsync("Administrator"))
        {
            await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole<Guid> { Name = "Administrator", NormalizedName = "ADMINISTRATOR" });
        }

        var testTenant = new BARQ.Core.Entities.Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        if (!context.Tenants.Any())
        {
            context.Tenants.Add(testTenant);
            await context.SaveChangesAsync();
        }
        else
        {
            testTenant = await context.Tenants.FirstAsync();
        }

        var testUser = await userManager.FindByEmailAsync("admin@barq.com");
        if (testUser == null)
        {
            testUser = new BARQ.Core.Entities.ApplicationUser
            {
                UserName = "admin@barq.com",
                Email = "admin@barq.com",
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = "Admin",
                TenantId = testTenant.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await userManager.CreateAsync(testUser, "Admin@123456");
            await userManager.AddToRoleAsync(testUser, "Administrator");
        }
    }
}
