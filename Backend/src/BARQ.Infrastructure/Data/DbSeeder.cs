using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BARQ.Core.Entities;

namespace BARQ.Infrastructure.Data;

public static class DbSeeder
{
    public static async System.Threading.Tasks.Task SeedAsync(BarqDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager)
    {
        await SeedRolesAsync(roleManager);
        await SeedSystemTenantAsync(context);
        await SeedUsersAsync(userManager, context);
        await SeedBillingPlansAsync(context);
        await SeedSystemConfigurationsAsync(context);
        await SeedLanguagesAsync(context);
        
        await context.SaveChangesAsync();
    }

    private static async System.Threading.Tasks.Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        var roles = new[] { "Administrator", "Manager", "User" };
        
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid> { Name = role, NormalizedName = role.ToUpper() });
            }
        }
    }

    private static async System.Threading.Tasks.Task SeedSystemTenantAsync(BarqDbContext context)
    {
        if (!await context.Tenants.AnyAsync())
        {
            var systemTenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "System",
                DisplayName = "System Tenant",
                Description = "Default system tenant for administrative purposes",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            
            context.Tenants.Add(systemTenant);
        }
    }

    private static async System.Threading.Tasks.Task SeedUsersAsync(UserManager<ApplicationUser> userManager, BarqDbContext context)
    {
        var systemTenant = await context.Tenants.FirstAsync(t => t.Name == "System");
        
        if (!await userManager.Users.AnyAsync())
        {
            var adminUser = new ApplicationUser
            {
                UserName = "admin@barq.com",
                Email = "admin@barq.com",
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Administrator",
                TenantId = systemTenant.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            
            await userManager.CreateAsync(adminUser, "Admin@123456");
            await userManager.AddToRoleAsync(adminUser, "Administrator");
        }
    }

    private static async System.Threading.Tasks.Task SeedBillingPlansAsync(BarqDbContext context)
    {
        if (!await context.BillingPlans.AnyAsync())
        {
            var plans = new[]
            {
                new BillingPlan { Name = "Free", Price = 0, MaxUsers = 5, MaxProjects = 10, MaxStorageBytes = 1073741824, IsActive = true }, // 1GB
                new BillingPlan { Name = "Professional", Price = 29.99m, MaxUsers = 25, MaxProjects = 100, MaxStorageBytes = 10737418240, IsActive = true }, // 10GB
                new BillingPlan { Name = "Enterprise", Price = 99.99m, MaxUsers = 0, MaxProjects = 0, MaxStorageBytes = 0, IsActive = true } // Unlimited
            };
            
            context.BillingPlans.AddRange(plans);
        }
    }

    private static async System.Threading.Tasks.Task SeedSystemConfigurationsAsync(BarqDbContext context)
    {
        if (!await context.SystemConfigurations.AnyAsync())
        {
            var configs = new[]
            {
                new SystemConfiguration { Key = "MaxFileUploadSize", Value = "104857600", Description = "Maximum file upload size in bytes" },
                new SystemConfiguration { Key = "SupportedFileTypes", Value = ".pdf,.doc,.docx,.txt,.jpg,.png,.gif,.zip", Description = "Supported file types for upload" },
                new SystemConfiguration { Key = "DefaultSLAHours", Value = "24", Description = "Default SLA in hours" },
                new SystemConfiguration { Key = "EnableAuditLogging", Value = "true", Description = "Enable audit logging" }
            };
            
            context.SystemConfigurations.AddRange(configs);
        }
    }

    private static async System.Threading.Tasks.Task SeedLanguagesAsync(BarqDbContext context)
    {
        if (!await context.Languages.AnyAsync())
        {
            var languages = new[]
            {
                new Language { Code = "en", Name = "English", NativeName = "English", Direction = "ltr", IsEnabled = true, IsDefault = true },
                new Language { Code = "ar", Name = "Arabic", NativeName = "العربية", Direction = "rtl", IsEnabled = true, IsDefault = false },
                new Language { Code = "es", Name = "Spanish", NativeName = "Español", Direction = "ltr", IsEnabled = true, IsDefault = false }
            };
            
            context.Languages.AddRange(languages);
        }
    }
}
