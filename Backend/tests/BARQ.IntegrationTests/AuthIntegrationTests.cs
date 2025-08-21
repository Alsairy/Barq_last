using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using BARQ.Core.DTOs;

namespace BARQ.IntegrationTests;

public class AuthIntegrationTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthIntegrationTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccessAndSetsCookie()
    {
        var loginRequest = new LoginRequest
        {
            UserName = "admin@barq.com",
            Email = "admin@barq.com",
            Password = "Admin@123456"
        };

        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/auth/login", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Login failed with status {response.StatusCode}: {errorContent}");
        }

        response.Should().BeSuccessful();
        response.Headers.Should().ContainKey("Set-Cookie");
        
        var cookies = response.Headers.GetValues("Set-Cookie");
        cookies.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/tasks");

        response.StatusCode.Should().BeOneOf(System.Net.HttpStatusCode.Unauthorized, System.Net.HttpStatusCode.Found);
    }

    [Theory]
    [InlineData("/api/auth/login")]
    [InlineData("/health")]
    [InlineData("/swagger")]
    public async Task PublicEndpoints_AllowAnonymousAccess(string endpoint)
    {
        var response = await _client.GetAsync(endpoint);

        response.StatusCode.Should().NotBe(System.Net.HttpStatusCode.Unauthorized);
    }
}
