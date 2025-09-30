using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Api.Models.Auth;
using SmartUnderwrite.Api.Constants;
using SmartUnderwrite.Infrastructure.Data;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;

namespace SmartUnderwrite.IntegrationTests.Controllers;

public class AuthControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccessAndToken()
    {
        // Arrange
        await SeedTestUserAsync();
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "TestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(loginResponse);
        Assert.NotEmpty(loginResponse.AccessToken);
        Assert.NotEmpty(loginResponse.RefreshToken);
        Assert.Equal("test@example.com", loginResponse.User.Email);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "invalid-email",
            Password = "TestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewTokens()
    {
        // Arrange
        await SeedTestUserAsync();
        var loginResponse = await LoginTestUserAsync();
        
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = loginResponse.RefreshToken
        };

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var newLoginResponse = JsonSerializer.Deserialize<LoginResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(newLoginResponse);
        Assert.NotEmpty(newLoginResponse.AccessToken);
        Assert.NotEmpty(newLoginResponse.RefreshToken);
        Assert.NotEqual(loginResponse.AccessToken, newLoginResponse.AccessToken);
        Assert.NotEqual(loginResponse.RefreshToken, newLoginResponse.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = "invalid-refresh-token"
        };

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-access-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        await SeedTestUserAsync();
        var loginResponse = await LoginTestUserAsync();
        
        var logoutRequest = new RefreshTokenRequest
        {
            RefreshToken = loginResponse.RefreshToken
        };

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/logout", logoutRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var logoutRequest = new RefreshTokenRequest
        {
            RefreshToken = "some-refresh-token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/logout", logoutRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsUserInfo()
    {
        // Arrange
        await SeedTestUserAsync();
        var loginResponse = await LoginTestUserAsync();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var userInfo = JsonSerializer.Deserialize<UserInfo>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(userInfo);
        Assert.Equal("test@example.com", userInfo.Email);
        Assert.Equal("Test", userInfo.FirstName);
        Assert.Equal("User", userInfo.LastName);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_AsAdmin_WithValidData_ReturnsSuccess()
    {
        // Arrange
        await SeedAdminUserAsync();
        var adminLoginResponse = await LoginAdminUserAsync();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminLoginResponse.AccessToken);

        var registerRequest = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "NewPassword123!",
            ConfirmPassword = "NewPassword123!",
            FirstName = "New",
            LastName = "User",
            Role = Roles.Underwriter
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(loginResponse);
        Assert.NotEmpty(loginResponse.AccessToken);
        Assert.Equal("newuser@example.com", loginResponse.User.Email);
        Assert.Contains(Roles.Underwriter, loginResponse.User.Roles);
    }

    [Fact]
    public async Task Register_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        await SeedTestUserAsync();
        var loginResponse = await LoginTestUserAsync();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

        var registerRequest = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "NewPassword123!",
            ConfirmPassword = "NewPassword123!",
            FirstName = "New",
            LastName = "User",
            Role = Roles.Underwriter
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidRole_ReturnsBadRequest()
    {
        // Arrange
        await SeedAdminUserAsync();
        var adminLoginResponse = await LoginAdminUserAsync();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminLoginResponse.AccessToken);

        var registerRequest = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "NewPassword123!",
            ConfirmPassword = "NewPassword123!",
            FirstName = "New",
            LastName = "User",
            Role = "InvalidRole"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task SeedTestUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var context = scope.ServiceProvider.GetRequiredService<SmartUnderwriteDbContext>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Ensure role exists
        if (!await roleManager.RoleExistsAsync(Roles.Underwriter))
        {
            await roleManager.CreateAsync(new Role(Roles.Underwriter));
        }

        // Check if user already exists
        var existingUser = await userManager.FindByEmailAsync("test@example.com");
        if (existingUser != null)
        {
            return;
        }

        var user = new User
        {
            UserName = "test@example.com",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user, "TestPassword123!");
        await userManager.AddToRoleAsync(user, Roles.Underwriter);
    }

    private async Task SeedAdminUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var context = scope.ServiceProvider.GetRequiredService<SmartUnderwriteDbContext>();

        // Ensure database is created
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

    private async Task<LoginResponse> LoginTestUserAsync()
    {
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "TestPassword123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var content = await response.Content.ReadAsStringAsync();
        
        return JsonSerializer.Deserialize<LoginResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
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
}