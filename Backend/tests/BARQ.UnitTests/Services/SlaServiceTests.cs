using Xunit;
using BARQ.Application.Services.Workflow;

namespace BARQ.UnitTests.Services;

public class SlaServiceTests
{
    private readonly SlaService _slaService;

    public SlaServiceTests()
    {
        _slaService = new SlaService();
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
