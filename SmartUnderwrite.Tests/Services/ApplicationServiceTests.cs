using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartUnderwrite.Api.Models.Application;
using SmartUnderwrite.Api.Services;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Core.Enums;
using SmartUnderwrite.Core.ValueObjects;
using SmartUnderwrite.Infrastructure.Data;
using System.Security.Claims;
using Xunit;

namespace SmartUnderwrite.Tests.Services;

public class ApplicationServiceTests : IDisposable
{
    private readonly SmartUnderwriteDbContext _context;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<ILogger<ApplicationService>> _mockLogger;
    private readonly ApplicationService _service;

    public ApplicationServiceTests()
    {
        var options = new DbContextOptionsBuilder<SmartUnderwriteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SmartUnderwriteDbContext(options);
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockLogger = new Mock<ILogger<ApplicationService>>();
        
        _service = new ApplicationService(_context, _mockCurrentUserService.Object, _mockLogger.Object);

        SeedTestData();
    }

    [Fact]
    public async Task CreateApplicationAsync_ValidRequest_CreatesApplication()
    {
        // Arrange
        var request = new CreateApplicationRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Ssn = "123-45-6789",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "555-123-4567",
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
        var result = await _service.CreateApplicationAsync(request, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result.Applicant.FirstName);
        Assert.Equal("Doe", result.Applicant.LastName);
        Assert.Equal("Personal Loan", result.ProductType);
        Assert.Equal(10000, result.Amount);
        Assert.Equal(ApplicationStatus.Submitted, result.Status);
        Assert.Equal(1, result.AffiliateId);

        // Verify in database
        var applicationInDb = await _context.LoanApplications
            .Include(la => la.Applicant)
            .FirstOrDefaultAsync(la => la.Id == result.Id);
        
        Assert.NotNull(applicationInDb);
        Assert.Equal("john.doe@example.com", applicationInDb.Applicant.Email);
    }

    [Fact]
    public async Task CreateApplicationAsync_InactiveAffiliate_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateApplicationRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Ssn = "123-45-6789",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "555-123-4567",
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
            EmploymentType = "Full-time"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.CreateApplicationAsync(request, 999));
    }

    [Fact]
    public async Task GetApplicationAsync_AdminUser_ReturnsApplication()
    {
        // Arrange
        var adminUser = CreateClaimsPrincipal("admin@test.com", new[] { "Admin" });

        // Act
        var result = await _service.GetApplicationAsync(1, adminUser);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetApplicationAsync_AffiliateUser_ReturnsOwnApplication()
    {
        // Arrange
        var affiliateUser = CreateClaimsPrincipal("affiliate1@test.com", new[] { "Affiliate" });
        _mockCurrentUserService.Setup(x => x.GetAffiliateId()).Returns(1);

        // Act
        var result = await _service.GetApplicationAsync(1, affiliateUser);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(1, result.AffiliateId);
    }

    [Fact]
    public async Task GetApplicationAsync_AffiliateUser_CannotAccessOtherAffiliateApplication()
    {
        // Arrange
        var affiliateUser = CreateClaimsPrincipal("affiliate1@test.com", new[] { "Affiliate" });
        _mockCurrentUserService.Setup(x => x.GetAffiliateId()).Returns(1);

        // Act
        var result = await _service.GetApplicationAsync(2, affiliateUser); // Application belongs to affiliate 2

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetApplicationsAsync_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var adminUser = CreateClaimsPrincipal("admin@test.com", new[] { "Admin" });
        var filter = new ApplicationFilter
        {
            Status = ApplicationStatus.Submitted,
            MinAmount = 5000,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetApplicationsAsync(filter, adminUser);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Items.Count > 0);
        Assert.All(result.Items, app => 
        {
            Assert.Equal(ApplicationStatus.Submitted, app.Status);
            Assert.True(app.Amount >= 5000);
        });
    }

    [Fact]
    public async Task GetApplicationsAsync_AffiliateUser_OnlyReturnsOwnApplications()
    {
        // Arrange
        var affiliateUser = CreateClaimsPrincipal("affiliate1@test.com", new[] { "Affiliate" });
        _mockCurrentUserService.Setup(x => x.GetAffiliateId()).Returns(1);
        var filter = new ApplicationFilter { Page = 1, PageSize = 10 };

        // Act
        var result = await _service.GetApplicationsAsync(filter, affiliateUser);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Items, app => Assert.Equal(1, app.AffiliateId));
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_ValidRequest_UpdatesStatus()
    {
        // Arrange
        var adminUser = CreateClaimsPrincipal("admin@test.com", new[] { "Admin" });

        // Act
        var result = await _service.UpdateApplicationStatusAsync(1, ApplicationStatus.Evaluated, adminUser);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ApplicationStatus.Evaluated, result.Status);
        Assert.NotNull(result.UpdatedAt);

        // Verify in database
        var applicationInDb = await _context.LoanApplications.FindAsync(1);
        Assert.NotNull(applicationInDb);
        Assert.Equal(ApplicationStatus.Evaluated, applicationInDb.Status);
    }

    private void SeedTestData()
    {
        // Create affiliates
        var affiliate1 = new Affiliate
        {
            Id = 1,
            Name = "Test Affiliate 1",
            ExternalId = "TEST001",
            IsActive = true
        };

        var affiliate2 = new Affiliate
        {
            Id = 2,
            Name = "Test Affiliate 2",
            ExternalId = "TEST002",
            IsActive = true
        };

        _context.Affiliates.AddRange(affiliate1, affiliate2);

        // Create applicants
        var applicant1 = new Applicant
        {
            Id = 1,
            FirstName = "Test",
            LastName = "User1",
            SsnHash = "hash1",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "555-0001",
            Email = "test1@example.com",
            Address = new Address
            {
                Street = "123 Test St",
                City = "Test City",
                State = "TS",
                ZipCode = "12345"
            }
        };

        var applicant2 = new Applicant
        {
            Id = 2,
            FirstName = "Test",
            LastName = "User2",
            SsnHash = "hash2",
            DateOfBirth = new DateTime(1985, 5, 15),
            Phone = "555-0002",
            Email = "test2@example.com",
            Address = new Address
            {
                Street = "456 Test Ave",
                City = "Test Town",
                State = "TS",
                ZipCode = "67890"
            }
        };

        _context.Applicants.AddRange(applicant1, applicant2);

        // Create loan applications
        var application1 = new LoanApplication
        {
            Id = 1,
            AffiliateId = 1,
            ApplicantId = 1,
            ProductType = "Personal Loan",
            Amount = 10000,
            IncomeMonthly = 5000,
            EmploymentType = "Full-time",
            CreditScore = 720,
            Status = ApplicationStatus.Submitted
        };

        var application2 = new LoanApplication
        {
            Id = 2,
            AffiliateId = 2,
            ApplicantId = 2,
            ProductType = "Auto Loan",
            Amount = 25000,
            IncomeMonthly = 7500,
            EmploymentType = "Full-time",
            CreditScore = 680,
            Status = ApplicationStatus.Submitted
        };

        _context.LoanApplications.AddRange(application1, application2);
        _context.SaveChanges();
    }

    private static ClaimsPrincipal CreateClaimsPrincipal(string email, string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new(ClaimTypes.NameIdentifier, "1")
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}