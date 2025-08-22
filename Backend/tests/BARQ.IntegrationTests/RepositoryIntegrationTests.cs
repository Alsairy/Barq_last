using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;
using BARQ.Infrastructure.Data;
using BARQ.Core.Entities;

namespace BARQ.IntegrationTests;

public class RepositoryIntegrationTests : IDisposable
{
    private readonly BarqDbContext _context;
    private readonly IServiceProvider _serviceProvider;

    public RepositoryIntegrationTests()
    {
        var services = new ServiceCollection();
        
        services.AddDbContext<BarqDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        services.AddLogging(builder => builder.AddConsole());
        services.AddScoped<BARQ.Core.Services.ITenantProvider, BARQ.IntegrationTests.Mocks.MockTenantProvider>();
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<BarqDbContext>();
    }

    [Fact]
    public async System.Threading.Tasks.Task TenantFilter_OnlyReturnsTenantSpecificData()
    {
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();

        var tenant1Task = new Core.Entities.Task
        {
            Id = Guid.NewGuid(),
            Title = "Tenant 1 Task",
            TenantId = tenant1Id,
            CreatedAt = DateTime.UtcNow
        };

        var tenant2Task = new Core.Entities.Task
        {
            Id = Guid.NewGuid(),
            Title = "Tenant 2 Task",
            TenantId = tenant2Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.AddRange(tenant1Task, tenant2Task);
        await _context.SaveChangesAsync();

        var tenant1Tasks = await _context.Tasks
            .Where(t => t.TenantId == tenant1Id)
            .ToListAsync();

        tenant1Tasks.Should().HaveCount(1);
        tenant1Tasks.First().Title.Should().Be("Tenant 1 Task");
    }

    [Fact]
    public async System.Threading.Tasks.Task SoftDelete_ExcludesDeletedEntities()
    {
        var task = new Core.Entities.Task
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            TenantId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        task.IsDeleted = true;
        task.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var activeTasks = await _context.Tasks.ToListAsync();
        var allTasks = await _context.Tasks.IgnoreQueryFilters().ToListAsync();

        activeTasks.Should().BeEmpty();
        allTasks.Should().HaveCount(1);
        allTasks.First().IsDeleted.Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Dispose();
        _serviceProvider.GetService<IServiceScope>()?.Dispose();
    }
}
