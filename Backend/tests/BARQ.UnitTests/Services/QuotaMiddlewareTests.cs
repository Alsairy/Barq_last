using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using BARQ.Application.Services;
using BARQ.Infrastructure.Data;
using BARQ.Core.Entities;
using BARQ.Core.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BARQ.UnitTests.Mocks;

namespace BARQ.UnitTests.Services;

public class QuotaMiddlewareTests : IDisposable
{
    private readonly BarqDbContext _context;
    private readonly Mock<ILogger<QuotaMiddleware>> _loggerMock;
    private readonly QuotaMiddleware _middleware;
    private readonly Mock<RequestDelegate> _nextMock;

    public QuotaMiddlewareTests()
    {
        var options = new DbContextOptionsBuilder<BarqDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var mockTenantProvider = new MockTenantProvider();
        _context = new BarqDbContext(options, mockTenantProvider);
        _loggerMock = new Mock<ILogger<QuotaMiddleware>>();
        _nextMock = new Mock<RequestDelegate>();
        _middleware = new QuotaMiddleware(_context, _loggerMock.Object);
    }

    [Fact]
    public async System.Threading.Tasks.Task InvokeAsync_WithinQuota_CallsNext()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Test Tenant",
            IsActive = true
        };

        var subscription = new TenantSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BillingPlanId = Guid.NewGuid(),
            Status = "Active",
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30),
            NextBillingDate = DateTime.UtcNow.AddDays(30),
            CurrentPrice = 29.99m
        };

        var quota = new UsageQuota
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            QuotaType = "API_CALLS",
            QuotaLimit = 1000,
            CurrentUsage = 500,
            NextResetDate = DateTime.UtcNow.AddDays(30),
            ResetPeriod = "Monthly",
            IsActive = true
        };

        _context.Tenants.Add(tenant);
        _context.TenantSubscriptions.Add(subscription);
        _context.UsageQuotas.Add(quota);
        await _context.SaveChangesAsync();

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("TenantId", tenantId.ToString()),
            new Claim("sub", userId.ToString())
        }));
        httpContext.Request.Path = "/api/tasks";
        httpContext.Request.Method = "POST";

        var result = await _middleware.CheckQuotaAsync(tenantId, "API_CALLS");

        result.Should().BeTrue();
    }

    [Fact]
    public async System.Threading.Tasks.Task InvokeAsync_OverQuota_Returns402()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Test Tenant",
            IsActive = true
        };

        var subscription = new TenantSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BillingPlanId = Guid.NewGuid(),
            Status = "Active",
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30),
            NextBillingDate = DateTime.UtcNow.AddDays(30),
            CurrentPrice = 29.99m
        };

        var quota = new UsageQuota
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            QuotaType = "API_CALLS",
            QuotaLimit = 1000,
            CurrentUsage = 1000, // At limit
            NextResetDate = DateTime.UtcNow.AddDays(30),
            ResetPeriod = "Monthly",
            IsActive = true
        };

        _context.Tenants.Add(tenant);
        _context.TenantSubscriptions.Add(subscription);
        _context.UsageQuotas.Add(quota);
        await _context.SaveChangesAsync();

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("TenantId", tenantId.ToString()),
            new Claim("sub", userId.ToString())
        }));
        httpContext.Request.Path = "/api/tasks";
        httpContext.Request.Method = "POST";
        httpContext.Response.Body = new MemoryStream();

        var result = await _middleware.CheckQuotaAsync(tenantId, "API_CALLS");

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("/health", true)]
    [InlineData("/api/auth/login", true)]
    [InlineData("/api/tasks", false)]
    [InlineData("/swagger", true)]
    public void IsExemptEndpoint_WithDifferentPaths_ReturnsExpectedResult(string path, bool expectedExempt)
    {
        var exemptPaths = new[] { "/health", "/api/auth", "/swagger" };

        var isExempt = exemptPaths.Any(exemptPath => path.StartsWith(exemptPath, StringComparison.OrdinalIgnoreCase));

        isExempt.Should().Be(expectedExempt);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
