using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using BARQ.Application.Services.Workflow;
using BARQ.Infrastructure.Data;
using BARQ.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BARQ.UnitTests.Services;

public class SlaServiceTests : IDisposable
{
    private readonly BarqDbContext _context;
    private readonly Mock<ILogger<SlaService>> _loggerMock;
    private readonly SlaService _slaService;

    public SlaServiceTests()
    {
        var options = new DbContextOptionsBuilder<BarqDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new BarqDbContext(options);
        _loggerMock = new Mock<ILogger<SlaService>>();
        _slaService = new SlaService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task CalculateRemainingTime_WithValidTask_ReturnsCorrectTime()
    {
        var task = new Core.Entities.Task
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            Priority = "High",
            Status = "InProgress"
        };
        
        var slaPolicy = new SlaPolicy
        {
            Id = Guid.NewGuid(),
            Priority = "High",
            ResponseTimeHours = 4,
            ResolutionTimeHours = 24,
            IsActive = true
        };

        _context.Tasks.Add(task);
        _context.SlaPolicies.Add(slaPolicy);
        await _context.SaveChangesAsync();

        var remainingTime = await _slaService.CalculateRemainingTimeAsync(task.Id);

        remainingTime.Should().BeCloseTo(TimeSpan.FromHours(2), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CalculateRemainingTime_WithExpiredTask_ReturnsNegativeTime()
    {
        var task = new Core.Entities.Task
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddHours(-6),
            Priority = "High",
            Status = "InProgress"
        };
        
        var slaPolicy = new SlaPolicy
        {
            Id = Guid.NewGuid(),
            Priority = "High",
            ResponseTimeHours = 4,
            ResolutionTimeHours = 24,
            IsActive = true
        };

        _context.Tasks.Add(task);
        _context.SlaPolicies.Add(slaPolicy);
        await _context.SaveChangesAsync();

        var remainingTime = await _slaService.CalculateRemainingTimeAsync(task.Id);

        remainingTime.Should().BeNegative();
    }

    [Theory]
    [InlineData("Low", 8)]
    [InlineData("Medium", 6)]
    [InlineData("High", 4)]
    [InlineData("Critical", 2)]
    public async Task CalculateRemainingTime_WithDifferentPriorities_ReturnsCorrectSlaTime(string priority, int expectedHours)
    {
        var task = new Core.Entities.Task
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            Priority = priority,
            Status = "InProgress"
        };
        
        var slaPolicy = new SlaPolicy
        {
            Id = Guid.NewGuid(),
            Priority = priority,
            ResponseTimeHours = expectedHours,
            ResolutionTimeHours = 24,
            IsActive = true
        };

        _context.Tasks.Add(task);
        _context.SlaPolicies.Add(slaPolicy);
        await _context.SaveChangesAsync();

        var remainingTime = await _slaService.CalculateRemainingTimeAsync(task.Id);

        remainingTime.Should().BeCloseTo(TimeSpan.FromHours(expectedHours - 1), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CheckViolations_WithExpiredTasks_CreatesViolations()
    {
        var expiredTask = new Core.Entities.Task
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddHours(-6),
            Priority = "High",
            Status = "InProgress"
        };
        
        var slaPolicy = new SlaPolicy
        {
            Id = Guid.NewGuid(),
            Priority = "High",
            ResponseTimeHours = 4,
            ResolutionTimeHours = 24,
            IsActive = true
        };

        _context.Tasks.Add(expiredTask);
        _context.SlaPolicies.Add(slaPolicy);
        await _context.SaveChangesAsync();

        var violations = await _slaService.CheckViolationsAsync();

        violations.Should().HaveCount(1);
        violations.First().TaskId.Should().Be(expiredTask.Id);
        violations.First().ViolationType.Should().Be("ResponseTime");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
