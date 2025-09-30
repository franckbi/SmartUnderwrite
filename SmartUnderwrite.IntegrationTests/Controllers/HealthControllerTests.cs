using System.Net;
using System.Text.Json;

namespace SmartUnderwrite.IntegrationTests.Controllers;

public class HealthControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public HealthControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/api/health/healthz");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var healthResponse = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.True(healthResponse.TryGetProperty("Status", out var status));
        Assert.Equal("Healthy", status.GetString());
        
        Assert.True(healthResponse.TryGetProperty("Timestamp", out _));
        Assert.True(healthResponse.TryGetProperty("Version", out _));
        Assert.True(healthResponse.TryGetProperty("Environment", out _));
    }

    [Fact]
    public async Task ReadinessCheck_ReturnsReady()
    {
        // Act
        var response = await _client.GetAsync("/api/health/readyz");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var readinessResponse = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.True(readinessResponse.TryGetProperty("Status", out var status));
        Assert.Equal("Ready", status.GetString());
        
        Assert.True(readinessResponse.TryGetProperty("DatabaseConnected", out var dbConnected));
        Assert.True(dbConnected.GetBoolean());
        
        Assert.True(readinessResponse.TryGetProperty("Timestamp", out _));
        Assert.True(readinessResponse.TryGetProperty("Version", out _));
    }

    [Fact]
    public async Task GetMetrics_ReturnsMetrics()
    {
        // Act
        var response = await _client.GetAsync("/api/health/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var metricsResponse = JsonSerializer.Deserialize<JsonElement>(content);

        // Check main sections exist
        Assert.True(metricsResponse.TryGetProperty("Application", out var application));
        Assert.True(metricsResponse.TryGetProperty("System", out var system));
        Assert.True(metricsResponse.TryGetProperty("Database", out var database));
        Assert.True(metricsResponse.TryGetProperty("Timestamp", out _));

        // Check application info
        Assert.True(application.TryGetProperty("Name", out var name));
        Assert.Equal("SmartUnderwrite API", name.GetString());
        
        Assert.True(application.TryGetProperty("Version", out _));
        Assert.True(application.TryGetProperty("Environment", out _));
        Assert.True(application.TryGetProperty("StartTime", out _));
        Assert.True(application.TryGetProperty("Uptime", out _));

        // Check system info
        Assert.True(system.TryGetProperty("MachineName", out _));
        Assert.True(system.TryGetProperty("ProcessorCount", out _));
        Assert.True(system.TryGetProperty("WorkingSet", out _));
        Assert.True(system.TryGetProperty("ThreadCount", out _));
    }

    [Fact]
    public async Task GetApplicationInfo_ReturnsInfo()
    {
        // Act
        var response = await _client.GetAsync("/api/health/info");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var infoResponse = JsonSerializer.Deserialize<JsonElement>(content);

        // Check main sections exist
        Assert.True(infoResponse.TryGetProperty("Application", out var application));
        Assert.True(infoResponse.TryGetProperty("Features", out var features));
        Assert.True(infoResponse.TryGetProperty("Endpoints", out var endpoints));

        // Check application info
        Assert.True(application.TryGetProperty("Name", out var name));
        Assert.Equal("SmartUnderwrite API", name.GetString());
        
        Assert.True(application.TryGetProperty("Description", out _));
        Assert.True(application.TryGetProperty("Version", out _));

        // Check features
        Assert.True(features.TryGetProperty("Authentication", out _));
        Assert.True(features.TryGetProperty("Authorization", out _));
        Assert.True(features.TryGetProperty("Database", out _));
        Assert.True(features.TryGetProperty("RulesEngine", out _));

        // Check endpoints
        Assert.True(endpoints.TryGetProperty("Health", out var health));
        Assert.Equal("/api/health/healthz", health.GetString());
        
        Assert.True(endpoints.TryGetProperty("Readiness", out var readiness));
        Assert.Equal("/api/health/readyz", readiness.GetString());
    }

    [Fact]
    public async Task DetailedHealthCheck_ReturnsDetailedStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/health/detailed");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable);
        
        var content = await response.Content.ReadAsStringAsync();
        var detailedResponse = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.True(detailedResponse.TryGetProperty("Status", out var status));
        var statusValue = status.GetString();
        Assert.True(statusValue == "Healthy" || statusValue == "Warning" || statusValue == "Unhealthy");
        
        Assert.True(detailedResponse.TryGetProperty("Checks", out var checks));
        Assert.True(detailedResponse.TryGetProperty("Timestamp", out _));
        Assert.True(detailedResponse.TryGetProperty("Version", out _));

        // Check that individual checks exist
        Assert.True(checks.TryGetProperty("Database", out var databaseCheck));
        Assert.True(checks.TryGetProperty("Memory", out var memoryCheck));
        Assert.True(checks.TryGetProperty("Threads", out var threadsCheck));

        // Each check should have a Status property
        Assert.True(databaseCheck.TryGetProperty("Status", out _));
        Assert.True(memoryCheck.TryGetProperty("Status", out _));
        Assert.True(threadsCheck.TryGetProperty("Status", out _));
    }

    [Fact]
    public async Task HealthEndpoints_AreAccessibleWithoutAuthentication()
    {
        // All health endpoints should be accessible without authentication
        var endpoints = new[]
        {
            "/api/health/healthz",
            "/api/health/readyz",
            "/api/health/metrics",
            "/api/health/info",
            "/api/health/detailed"
        };

        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            
            // Should not return Unauthorized
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            
            // Should return either OK or ServiceUnavailable (for health checks)
            Assert.True(
                response.StatusCode == HttpStatusCode.OK || 
                response.StatusCode == HttpStatusCode.ServiceUnavailable,
                $"Endpoint {endpoint} returned unexpected status code: {response.StatusCode}");
        }
    }
}