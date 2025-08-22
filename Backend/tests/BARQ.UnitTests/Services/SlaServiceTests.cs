using Xunit;
using BARQ.Application.Services.Workflow;
using BARQ.UnitTests.Mocks;
using Microsoft.EntityFrameworkCore;
using BARQ.Infrastructure.Data;

namespace BARQ.UnitTests.Services;

public class SlaServiceTests
{
    private readonly SlaService _slaService;
    private readonly BarqDbContext _context;
    private readonly MockTenantProvider _tenantProvider;

    public SlaServiceTests()
    {
        var options = new DbContextOptionsBuilder<BarqDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _tenantProvider = new MockTenantProvider();
        _context = new BarqDbContext(options, _tenantProvider);
        _slaService = new SlaService(_context, _tenantProvider);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetSlaPoliciesAsync_ShouldReturnEmptyResult()
    {
        var result = await _slaService.GetSlaPoliciesAsync();

        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetSlaPolicyByIdAsync_ShouldReturnNull()
    {
        var result = await _slaService.GetSlaPolicyByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateSlaPolicyAsync_ShouldReturnNewPolicy()
    {
        var policy = new BARQ.Core.Entities.SlaPolicy();

        var result = await _slaService.CreateSlaPolicyAsync(policy);

        Assert.NotNull(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task IsViolationAsync_ShouldReturnFalse()
    {
        var result = await _slaService.IsViolationAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result);
    }
}
