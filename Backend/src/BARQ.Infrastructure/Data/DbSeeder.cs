using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BARQ.Core.Entities;

namespace BARQ.Infrastructure.Data;

public sealed class DbSeeder
{


    public static async System.Threading.Tasks.Task SeedAsync(BarqDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager, CancellationToken ct = default)
    {
        await context.Database.EnsureCreatedAsync(ct);

        foreach (var r in new[] { "Admin", "Manager", "Viewer" })
            if (!await roleManager.RoleExistsAsync(r)) await roleManager.CreateAsync(new IdentityRole<Guid>(r));

        var admin = await userManager.FindByEmailAsync("admin@barq.local");
        if (admin is null)
        {
            admin = new ApplicationUser { Id = Guid.NewGuid(), Email = "admin@barq.local", UserName = "admin@barq.local", EmailConfirmed = true };
            await userManager.CreateAsync(admin, "ChangeMe!123");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        await context.SaveChangesAsync(ct);
    }
}
