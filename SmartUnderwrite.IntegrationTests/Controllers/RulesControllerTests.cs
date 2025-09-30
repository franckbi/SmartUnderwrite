using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SmartUnderwrite.Api.Constants;
using SmartUnderwrite.Api.Models.Auth;
using SmartUnderwrite.Api.Models.Rules;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Infrastructure.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SmartUnderwrite.IntegrationTests.Controllers;

public class RulesControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RulesControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllRules_AsAdmin_ReturnsSuccess()
    {
        // Arrange
        await SeedAdminUserAsync();
        var loginResponse = await LoginAdminUserAsync();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

        // Act
        var response = await _client.GetAsync("/api/rules");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var rules = JsonSerializer.Deserialize<List<RuleDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(rules);
    }

    [Fact]
    public async Task GetAllRules_AsAffiliate_ReturnsForbidden()
    {
        // Arrange
        await SeedAffiliateUserAsync();
        var loginResponse = await LoginAffiliateUserAsync();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

        // Act
        var response = await _client.GetAsync("/api/rules");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateRule_AsAdmin_ReturnsSuccess()
    {
        // Arrange
        await SeedAdminUserAsync();
        var loginResponse = await LoginAdminUserAsync();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

        var request = new CreateRuleRequest
        {
            Name = "Test Rule",
            Description = "A test rule for integration testing",
            RuleDefinition = """
            {
                "name": "Test Rule",
                "priority": 10,
                "clauses": [
                    {
                        "if": "CreditScore < 600",
                        "then": "REJECT",
                        "reason": "Low credit score"
                    }
                ],
                "score": {
                    "base": 500,
                    "add": []
                }
            }
            """,
            Priority = 10
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/rules", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var rule = JsonSerializer.Deserialize<RuleDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(rule);
        Assert.Equal(request.Name, rule.Name);
        Assert.Equal(request.Description, rule.Description);
        Assert.Equal(request.Priority, rule.Priority);
        Assert.True(rule.IsActive);
    }

    [Fact]
    public async Task CreateRule_AsUnderwriter_ReturnsForbidden()
    {
        // Arrange
        await SeedUnderwriterUserAsync();
        var loginResponse = await LoginUnderwriterUserAsync();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

        var request = new CreateRuleRequest
        {
            Name = "Test Rule",
            Description = "A test rule",
            RuleDefinition = "{}",
            Priority = 10
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/rules", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ValidateRule_WithValidDefinition_ReturnsValid()
    {
        // Arrange
        await SeedAdminUserAsync();
        var loginResponse = await LoginAdminUserAsync();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

        var request = new ValidateRuleRequest
        {
            RuleDefinition = """
            {
                "name": "Valid Rule",
                "priority": 10,
                "clauses": [
                    {
                        "if": "CreditScore >= 700",
                        "then": "APPROVE",
                        "reason": "Good credit score"
                    }
                ],
                "score": {
                    "base": 600,
                    "add": []
                }
            }
            """
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/rules/validate", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var validationResult = JsonSerializer.Deserialize<RuleValidationResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(validationResult);
        Assert.True(validationResult.IsValid);
        Assert.Empty(validationResult.Errors);
    }

    [Fact]
    public async Task ValidateRule_WithInvalidDefinition_ReturnsInvalid()
    {
        // Arrange
        await SeedAdminUserAsync();
        var loginResponse = await LoginAdminUserAsync();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

        var request = new ValidateRuleRequest
        {
            RuleDefinition = "invalid json"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/rules/validate", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var validationResult = JsonSerializer.Deserialize<RuleValidationResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(validationResult);
        Assert.False(validationResult.IsValid);
        Assert.NotEmpty(validationResult.Errors);
    }

    private async Task SeedAdminUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var context = scope.ServiceProvider.GetRequiredService<SmartUnderwriteDbContext>();

        await context.Database.EnsureCreatedAsync();

        // Ensure role exists
        if (!await roleManager.RoleExistsAsync(Roles.Admin))
        {
            await roleManager.CreateAsync(new Role(Roles.Admin));
        }

        // Check if user already exists
        var existingUser = await userManager.FindByEmailAsync("admin@example.com");
        if (existingUser != null)
        {
            return;
        }

        var user = new User
        {
            UserName = "admin@example.com",
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "User",
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user, "AdminPassword123!");
        await userManager.AddToRoleAsync(user, Roles.Admin);
    }

    private async Task SeedUnderwriterUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var context = scope.ServiceProvider.GetRequiredService<SmartUnderwriteDbContext>();

        await context.Database.EnsureCreatedAsync();

        // Ensure role exists
        if (!await roleManager.RoleExistsAsync(Roles.Underwriter))
        {
            await roleManager.CreateAsync(new Role(Roles.Underwriter));
        }

        // Check if user already exists
        var existingUser = await userManager.FindByEmailAsync("underwriter@example.com");
        if (existingUser != null)
        {
            return;
        }

        var user = new User
        {
            UserName = "underwriter@example.com",
            Email = "underwriter@example.com",
            FirstName = "Underwriter",
            LastName = "User",
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user, "UnderwriterPassword123!");
        await userManager.AddToRoleAsync(user, Roles.Underwriter);
    }

    private async Task SeedAffiliateUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var context = scope.ServiceProvider.GetRequiredService<SmartUnderwriteDbContext>();

        await context.Database.EnsureCreatedAsync();

        // Create affiliate
        var affiliate = new Affiliate
        {
            Name = "Test Affiliate",
            ExternalId = "TEST001",
            IsActive = true
        };
        context.Affiliates.Add(affiliate);
        await context.SaveChangesAsync();

        // Ensure role exists
        if (!await roleManager.RoleExistsAsync(Roles.Affiliate))
        {
            await roleManager.CreateAsync(new Role(Roles.Affiliate));
        }

        // Check if user already exists
        var existingUser = await userManager.FindByEmailAsync("affiliate@example.com");
        if (existingUser != null)
        {
            return;
        }

        var user = new User
        {
            UserName = "affiliate@example.com",
            Email = "affiliate@example.com",
            FirstName = "Affiliate",
            LastName = "User",
            AffiliateId = affiliate.Id,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user, "AffiliatePassword123!");
        await userManager.AddToRoleAsync(user, Roles.Affiliate);
    }

    private async Task<LoginResponse> LoginAdminUserAsync()
    {
        var loginRequest = new LoginRequest
        {
            Email = "admin@example.com",
            Password = "AdminPassword123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var content = await response.Content.ReadAsStringAsync();
        
        return JsonSerializer.Deserialize<LoginResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }

    private async Task<LoginResponse> LoginUnderwriterUserAsync()
    {
        var loginRequest = new LoginRequest
        {
            Email = "underwriter@example.com",
            Password = "UnderwriterPassword123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var content = await response.Content.ReadAsStringAsync();
        
        return JsonSerializer.Deserialize<LoginResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }

    private async Task<LoginResponse> LoginAffiliateUserAsync()
    {
        var loginRequest = new LoginRequest
        {
            Email = "affiliate@example.com",
            Password = "AffiliatePassword123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var content = await response.Content.ReadAsStringAsync();
        
        return JsonSerializer.Deserialize<LoginResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }
}