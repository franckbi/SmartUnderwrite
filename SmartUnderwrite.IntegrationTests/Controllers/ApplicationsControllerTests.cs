using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SmartUnderwrite.Api.Constants;
using SmartUnderwrite.Api.Models.Application;
using SmartUnderwrite.Api.Models.Auth;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Core.Enums;
using SmartUnderwrite.Infrastructure.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SmartUnderwrite.IntegrationTests.Controllers;

public class ApplicationsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApplicationsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateApplication_AsAffiliate_ReturnsSuccess()
    {
        // Arrange
        await SeedAffiliateUserAsync();
        var loginResponse = await LoginAffiliateUserAsync();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

        var request = new CreateApplicationRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Ssn = "123456789",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "555-1234",
            Email = "john.doe@example.com",
            Address = new AddressDto
            {
                Street = "123 Main St",
                City = "Anytown",
                State = "CA",
                ZipCode = "12345"
            },
            ProductType = "Personal Loan",
            Amount = 10000,
            IncomeMonthly = 5000,
            EmploymentType = "Full-time",
            CreditScore = 720
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/applications", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var application = JsonSerializer.Deserialize<LoanApplicationDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(application);
        Assert.Equal(request.FirstName, application.Applicant.FirstName);
        Assert.Equal(request.LastName, application.Applicant.LastName);
        Assert.Equal(request.Amount, application.Amount);
        Assert.Equal(ApplicationStatus.Submitted, application.Status);
    }

    [Fact]
    public async Task CreateApplication_AsUnderwriter_ReturnsForbidden()
    {
        // Arrange
        await SeedUnderwriterUserAsync();
        var loginResponse = await LoginUnderwriterUserAsync();

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

        var request = new CreateApplicationRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Ssn = "123456789",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "555-1234",
            Email = "john.doe@example.com",
            Address = new AddressDto
            {
                Street = "123 Main St",
                City = "Anytown",
                State = "CA",
                ZipCode = "12345"
            },
            ProductType = "Personal Loan",
            Amount = 10000,
            IncomeMonthly = 5000,
            EmploymentType = "Full-time",
            CreditScore = 720
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/applications", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetApplication_AsAffiliate_ReturnsOwnApplication()
    {
        // Arrange
        await SeedAffiliateUserAsync();
        var loginResponse = await LoginAffiliateUserAsync();
        var applicationId = await CreateTestApplicationAsync(loginResponse.AccessToken);

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

        // Act
        var response = await _client.GetAsync($"/api/applications/{applicationId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var application = JsonSerializer.Deserialize<LoanApplicationDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(application);
        Assert.Equal(applicationId, application.Id);
    }

    [Fact]
    public async Task GetApplications_AsAffiliate_ReturnsOwnApplicationsOnly()
    {
        // Arrange
        await SeedAffiliateUserAsync();
        var loginResponse = await LoginAffiliateUserAsync();
        await CreateTestApplicationAsync(loginResponse.AccessToken);

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

        // Act
        var response = await _client.GetAsync("/api/applications");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<LoanApplicationDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.True(result.Items.Count > 0);
        Assert.All(result.Items, app => Assert.Equal(1, app.AffiliateId)); // All should belong to affiliate 1
    }

    [Fact]
    public async Task GetApplications_AsUnderwriter_ReturnsAllApplications()
    {
        // Arrange
        await SeedUnderwriterUserAsync();
        await SeedAffiliateUserAsync();
        
        var affiliateLoginResponse = await LoginAffiliateUserAsync();
        await CreateTestApplicationAsync(affiliateLoginResponse.AccessToken);

        var underwriterLoginResponse = await LoginUnderwriterUserAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", underwriterLoginResponse.AccessToken);

        // Act
        var response = await _client.GetAsync("/api/applications");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<LoanApplicationDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.True(result.Items.Count > 0);
    }

    [Fact]
    public async Task EvaluateApplication_AsUnderwriter_ReturnsDecision()
    {
        // Arrange
        await SeedUnderwriterUserAsync();
        await SeedAffiliateUserAsync();
        
        var affiliateLoginResponse = await LoginAffiliateUserAsync();
        var applicationId = await CreateTestApplicationAsync(affiliateLoginResponse.AccessToken);

        var underwriterLoginResponse = await LoginUnderwriterUserAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", underwriterLoginResponse.AccessToken);

        // Act
        var response = await _client.PostAsync($"/api/applications/{applicationId}/evaluate", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var decision = JsonSerializer.Deserialize<DecisionDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(decision);
        Assert.Equal(applicationId, decision.LoanApplicationId);
        Assert.True(Enum.IsDefined(typeof(DecisionOutcome), decision.Outcome));
    }

    [Fact]
    public async Task EvaluateApplication_AsAffiliate_ReturnsForbidden()
    {
        // Arrange
        await SeedAffiliateUserAsync();
        var loginResponse = await LoginAffiliateUserAsync();
        var applicationId = await CreateTestApplicationAsync(loginResponse.AccessToken);

        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

        // Act
        var response = await _client.PostAsync($"/api/applications/{applicationId}/evaluate", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

    private async Task<int> CreateTestApplicationAsync(string accessToken)
    {
        var tempClient = _factory.CreateClient();
        tempClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var request = new CreateApplicationRequest
        {
            FirstName = "Test",
            LastName = "Applicant",
            Ssn = "987654321",
            DateOfBirth = new DateTime(1985, 5, 15),
            Phone = "555-9876",
            Email = "test.applicant@example.com",
            Address = new AddressDto
            {
                Street = "456 Test Ave",
                City = "Test City",
                State = "TX",
                ZipCode = "54321"
            },
            ProductType = "Auto Loan",
            Amount = 25000,
            IncomeMonthly = 6000,
            EmploymentType = "Full-time",
            CreditScore = 680
        };

        var response = await tempClient.PostAsJsonAsync("/api/applications", request);
        var content = await response.Content.ReadAsStringAsync();
        var application = JsonSerializer.Deserialize<LoanApplicationDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return application!.Id;
    }
}