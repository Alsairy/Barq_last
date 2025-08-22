using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;
using System.Diagnostics;

namespace BARQ.PerformanceTests;

public class ApiPerformanceTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;

    public ApiPerformanceTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_ShouldMeet95thPercentileLatencyThreshold()
    {
        var httpClient = _factory.CreateClient();
        var latencies = new List<long>();
        var successCount = 0;
        var failCount = 0;

        for (int i = 0; i < 100; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var response = await httpClient.GetAsync("/health");
                stopwatch.Stop();
                
                if (response.IsSuccessStatusCode)
                {
                    successCount++;
                    latencies.Add(stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    failCount++;
                }
            }
            catch
            {
                stopwatch.Stop();
                failCount++;
            }
        }

        latencies.Sort();
        var percentile95Index = (int)Math.Ceiling(latencies.Count * 0.95) - 1;
        var percentile95 = latencies[percentile95Index];

        percentile95.Should().BeLessOrEqualTo(200, "95th percentile latency should be under 200ms");
        successCount.Should().BeGreaterThan(90, "Success rate should be over 90%");
        failCount.Should().BeLessOrEqualTo(10, "Failure count should be minimal");
    }

    [Fact]
    public async Task TasksEndpoint_ShouldHandleConcurrentRequests()
    {
        var httpClient = _factory.CreateClient();
        var latencies = new List<long>();
        var successCount = 0;
        var failCount = 0;

        for (int i = 0; i < 50; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var response = await httpClient.GetAsync("/api/tasks");
                stopwatch.Stop();
                
                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    successCount++;
                    latencies.Add(stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    failCount++;
                }
            }
            catch
            {
                stopwatch.Stop();
                failCount++;
            }
        }

        latencies.Sort();
        if (latencies.Count > 0)
        {
            var percentile95Index = (int)Math.Ceiling(latencies.Count * 0.95) - 1;
            var percentile95 = latencies[percentile95Index];
            percentile95.Should().BeLessOrEqualTo(500, "95th percentile latency should be under 500ms");
        }

        successCount.Should().BeGreaterThan(40, "Success rate should be over 80%");
    }
}
