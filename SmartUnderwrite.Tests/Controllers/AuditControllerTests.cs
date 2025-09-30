using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartUnderwrite.Api.Models.Audit;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Infrastructure.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SmartUnderwrite.Tests.Controllers;

public class AuditControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly SmartUnderwriteDbContext _context;

    public AuditControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<SmartUnderwriteDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<SmartUnderwriteDbContext>(options =>
                {
                    options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                });
            });
        });

        _client = _factory.CreateClient();
        
        // Get the test database context
        var scope = _factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<SmartUnderwriteDbContext>();
    }

    [Fact]
    public async Task GetAuditLogs_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.GetAsync("/api/audit");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_ShouldReturnAuditLogs_WhenAuthenticatedAsAdmin()
    {
        // Arrange
        await SeedAuditLogs();
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/audit");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var auditLogs = JsonSerializer.Deserialize<List<AuditLogDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(auditLogs);
        Assert.Equal(3, auditLogs.Count);
    }

    [Fact]
    public async Task GetAuditLogs_ShouldFilterByEntityType_WhenEntityTypeProvided()
    {
        // Arrange
        await SeedAuditLogs();
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/audit?entityType=LoanApplication");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var auditLogs = JsonSerializer.Deserialize<List<AuditLogDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(auditLogs);
        Assert.Equal(2, auditLogs.Count);
        Assert.All(auditLogs, log => Assert.Equal("LoanApplication", log.EntityType));
    }

    [Fact]
    public async Task GetEntityAuditTrail_ShouldReturnTrail_WhenEntityExists()
    {
        // Arrange
        await SeedAuditLogs();
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/audit/trail/LoanApplication/123");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var auditTrail = JsonSerializer.Deserialize<List<AuditLogDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(auditTrail);
        Assert.Single(auditTrail);
        Assert.Equal("LoanApplication", auditTrail.First().EntityType);
        Assert.Equal("123", auditTrail.First().EntityId);
    }

    [Fact]
    public async Task GetAuditLog_ShouldReturnLog_WhenIdExists()
    {
        // Arrange
        await SeedAuditLogs();
        var existingLog = await _context.AuditLogs.FirstAsync();
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/audit/{existingLog.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var auditLog = JsonSerializer.Deserialize<AuditLogDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(auditLog);
        Assert.Equal(existingLog.Id, auditLog.Id);
        Assert.Equal(existingLog.EntityType, auditLog.EntityType);
    }

    [Fact]
    public async Task GetAuditLog_ShouldReturnNotFound_WhenIdNotExists()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/audit/999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditSummary_ShouldReturnSummary_WhenAuthenticatedAsAdmin()
    {
        // Arrange
        await SeedAuditLogs();
        var token = await GetAdminTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/audit/summary");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var summary = JsonSerializer.Deserialize<AuditSummaryDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(summary);
        Assert.Equal(3, summary.TotalEntries);
        Assert.Contains("CREATE", summary.ActionCounts.Keys);
        Assert.Contains("UPDATE", summary.ActionCounts.Keys);
        Assert.Contains("LoanApplication", summary.EntityTypeCounts.Keys);
        Assert.Contains("Rule", summary.EntityTypeCounts.Keys);
    }

    [Fact]
    public async Task GetAuditSummary_ShouldReturnForbidden_WhenAuthenticatedAsUnderwriter()
    {
        // Arrange
        var token = await GetUnderwriterTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/audit/summary");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task SeedAuditLogs()
    {
        var auditLogs = new[]
        {
            new AuditLog
            {
                EntityType = "LoanApplication",
                EntityId = "123",
                Action = "CREATE",
                Changes = "{\"amount\": 10000, \"status\": \"Submitted\"}",
                UserId = "1",
                Timestamp = DateTime.UtcNow.AddMinutes(-10)
            },
            new AuditLog
            {
                EntityType = "LoanApplication",
                EntityId = "456",
                Action = "UPDATE",
                Changes = "{\"status\": \"Approved\"}",
                UserId = "2",
                Timestamp = DateTime.UtcNow.AddMinutes(-5)
            },
            new AuditLog
            {
                EntityType = "Rule",
                EntityId = "789",
                Action = "DELETE",
                Changes = "{\"name\": \"Old Rule\"}",
                UserId = "1",
                Timestamp = DateTime.UtcNow.AddMinutes(-2)
            }
        };

        _context.AuditLogs.AddRange(auditLogs);
        await _context.SaveChangesAsync();
    }

    private async Task<string> GetAdminTokenAsync()
    {
        // This is a simplified token generation for testing
        // In a real implementation, you would authenticate with actual credentials
        var loginRequest = new
        {
            Email = "admin@smartunderwrite.com",
            Password = "Admin123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<JsonElement>(content);
            return loginResponse.GetProperty("token").GetString() ?? string.Empty;
        }

        // Return a mock token for testing if login fails
        return "mock-admin-token";
    }

    private async Task<string> GetUnderwriterTokenAsync()
    {
        // This is a simplified token generation for testing
        var loginRequest = new
        {
            Email = "underwriter@smartunderwrite.com",
            Password = "Underwriter123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<JsonElement>(content);
            return loginResponse.GetProperty("token").GetString() ?? string.Empty;
        }

        // Return a mock token for testing if login fails
        return "mock-underwriter-token";
    }

    public void Dispose()
    {
        _context.Dispose();
        _client.Dispose();
    }
}