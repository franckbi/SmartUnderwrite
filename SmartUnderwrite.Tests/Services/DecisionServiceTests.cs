using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartUnderwrite.Api.Models.Application;
using SmartUnderwrite.Api.Services;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Core.Enums;
using SmartUnderwrite.Core.RulesEngine.Interfaces;
using SmartUnderwrite.Core.RulesEngine.Models;
using SmartUnderwrite.Core.ValueObjects;
using SmartUnderwrite.Infrastructure.Data;
using System.Security.Claims;
using Xunit;

namespace SmartUnderwrite.Tests.Services;

public class DecisionServiceTests : IDisposable
{
    private readonly SmartUnderwriteDbContext _context;
    private readonly Mock<IRulesEngine> _mockRulesEngine;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<ILogger<DecisionService>> _mockLogger;
    private readonly DecisionService _decisionService;

    public DecisionServiceTests()
    {
        var options = new DbContextOptionsBuilder<SmartUnderwriteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SmartUnderwriteDbContext(options);
        _mockRulesEngine = new Mock<IRulesEngine>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockLogger = new Mock<ILogger<DecisionService>>();

        _decisionService = new DecisionService(
            _context,
            _mockRulesEngine.Object,
            _mockCurrentUserService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task EvaluateApplicationAsync_WithValidApplication_CreatesAutomatedDecision()
    {
        // Arrange
        var affiliate = new Affiliate { Id = 1, Name = "Test Affiliate", ExternalId = "TEST001", IsActive = true };
        var applicant = new Applicant
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            SsnHash = "hashedssn",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "555-1234",
            Email = "john@example.com",
            Address = new Address { Street = "123 Main St", City = "Anytown", State = "CA", ZipCode = "12345" }
        };
        var application = new LoanApplication
        {
            Id = 1,
            AffiliateId = 1,
            ApplicantId = 1,
            ProductType = "Personal Loan",
            Amount = 10000,
            IncomeMonthly = 5000,
            EmploymentType = "Full-time",
            CreditScore = 720,
            Status = ApplicationStatus.Submitted,
            Affiliate = affiliate,
            Applicant = applicant
        };

        _context.Affiliates.Add(affiliate);
        _context.Applicants.Add(applicant);
        _context.LoanApplications.Add(application);
        await _context.SaveChangesAsync();

        var evaluationResult = new EvaluationResult
        {
            Outcome = DecisionOutcome.Approve,
            Score = 750,
            Reasons = new List<string> { "Good credit score", "Stable income" }
        };

        _mockRulesEngine.Setup(x => x.EvaluateAsync(It.IsAny<LoanApplication>(), It.IsAny<Applicant>()))
            .ReturnsAsync(evaluationResult);

        // Act
        var result = await _decisionService.EvaluateApplicationAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DecisionOutcome.Approve, result.Outcome);
        Assert.Equal(750, result.Score);
        Assert.Equal(2, result.Reasons.Length);
        Assert.Contains("Good credit score", result.Reasons);
        Assert.Contains("Stable income", result.Reasons);
        Assert.Null(result.DecidedByUserId);
        Assert.True(result.IsAutomated);

        // Verify application status was updated
        var updatedApplication = await _context.LoanApplications.FindAsync(1);
        Assert.Equal(ApplicationStatus.Approved, updatedApplication!.Status);

        // Verify decision was saved
        var savedDecision = await _context.Decisions.FirstOrDefaultAsync(d => d.LoanApplicationId == 1);
        Assert.NotNull(savedDecision);
        Assert.Equal(DecisionOutcome.Approve, savedDecision.Outcome);
    }

    [Fact]
    public async Task EvaluateApplicationAsync_WithRejectedOutcome_UpdatesStatusToRejected()
    {
        // Arrange
        var affiliate = new Affiliate { Id = 1, Name = "Test Affiliate", ExternalId = "TEST001", IsActive = true };
        var applicant = new Applicant
        {
            Id = 1,
            FirstName = "Jane",
            LastName = "Doe",
            SsnHash = "hashedssn",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "555-1234",
            Email = "jane@example.com",
            Address = new Address { Street = "123 Main St", City = "Anytown", State = "CA", ZipCode = "12345" }
        };
        var application = new LoanApplication
        {
            Id = 1,
            AffiliateId = 1,
            ApplicantId = 1,
            ProductType = "Personal Loan",
            Amount = 10000,
            IncomeMonthly = 2000,
            EmploymentType = "Part-time",
            CreditScore = 500,
            Status = ApplicationStatus.Submitted,
            Affiliate = affiliate,
            Applicant = applicant
        };

        _context.Affiliates.Add(affiliate);
        _context.Applicants.Add(applicant);
        _context.LoanApplications.Add(application);
        await _context.SaveChangesAsync();

        var evaluationResult = new EvaluationResult
        {
            Outcome = DecisionOutcome.Reject,
            Score = 300,
            Reasons = new List<string> { "Low credit score" }
        };

        _mockRulesEngine.Setup(x => x.EvaluateAsync(It.IsAny<LoanApplication>(), It.IsAny<Applicant>()))
            .ReturnsAsync(evaluationResult);

        // Act
        var result = await _decisionService.EvaluateApplicationAsync(1);

        // Assert
        Assert.Equal(DecisionOutcome.Reject, result.Outcome);
        Assert.Equal(300, result.Score);

        // Verify application status was updated
        var updatedApplication = await _context.LoanApplications.FindAsync(1);
        Assert.Equal(ApplicationStatus.Rejected, updatedApplication!.Status);
    }

    [Fact]
    public async Task EvaluateApplicationAsync_WithManualReviewOutcome_UpdatesStatusToUnderReview()
    {
        // Arrange
        var affiliate = new Affiliate { Id = 1, Name = "Test Affiliate", ExternalId = "TEST001", IsActive = true };
        var applicant = new Applicant
        {
            Id = 1,
            FirstName = "Bob",
            LastName = "Smith",
            SsnHash = "hashedssn",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "555-1234",
            Email = "bob@example.com",
            Address = new Address { Street = "123 Main St", City = "Anytown", State = "CA", ZipCode = "12345" }
        };
        var application = new LoanApplication
        {
            Id = 1,
            AffiliateId = 1,
            ApplicantId = 1,
            ProductType = "Personal Loan",
            Amount = 75000,
            IncomeMonthly = 8000,
            EmploymentType = "Full-time",
            CreditScore = 650,
            Status = ApplicationStatus.Submitted,
            Affiliate = affiliate,
            Applicant = applicant
        };

        _context.Affiliates.Add(affiliate);
        _context.Applicants.Add(applicant);
        _context.LoanApplications.Add(application);
        await _context.SaveChangesAsync();

        var evaluationResult = new EvaluationResult
        {
            Outcome = DecisionOutcome.ManualReview,
            Score = 600,
            Reasons = new List<string> { "High amount risk" }
        };

        _mockRulesEngine.Setup(x => x.EvaluateAsync(It.IsAny<LoanApplication>(), It.IsAny<Applicant>()))
            .ReturnsAsync(evaluationResult);

        // Act
        var result = await _decisionService.EvaluateApplicationAsync(1);

        // Assert
        Assert.Equal(DecisionOutcome.ManualReview, result.Outcome);

        // Verify application status was updated
        var updatedApplication = await _context.LoanApplications.FindAsync(1);
        Assert.Equal(ApplicationStatus.InReview, updatedApplication!.Status);
    }

    [Fact]
    public async Task EvaluateApplicationAsync_WithNonExistentApplication_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _decisionService.EvaluateApplicationAsync(999));

        Assert.Contains("Application with ID 999 not found", exception.Message);
    }

    [Fact]
    public async Task EvaluateApplicationAsync_WithAlreadyApprovedApplication_ThrowsInvalidOperationException()
    {
        // Arrange
        var affiliate = new Affiliate { Id = 1, Name = "Test Affiliate", ExternalId = "TEST001", IsActive = true };
        var applicant = new Applicant
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            SsnHash = "hashedssn",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "555-1234",
            Email = "john@example.com",
            Address = new Address { Street = "123 Main St", City = "Anytown", State = "CA", ZipCode = "12345" }
        };
        var application = new LoanApplication
        {
            Id = 1,
            AffiliateId = 1,
            ApplicantId = 1,
            ProductType = "Personal Loan",
            Amount = 10000,
            IncomeMonthly = 5000,
            EmploymentType = "Full-time",
            CreditScore = 720,
            Status = ApplicationStatus.Approved, // Already approved
            Affiliate = affiliate,
            Applicant = applicant
        };

        _context.Affiliates.Add(affiliate);
        _context.Applicants.Add(applicant);
        _context.LoanApplications.Add(application);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _decisionService.EvaluateApplicationAsync(1));

        Assert.Contains("cannot be evaluated in status Approved", exception.Message);
    }

    [Fact]
    public async Task MakeManualDecisionAsync_WithValidRequest_CreatesManualDecision()
    {
        // Arrange
        var affiliate = new Affiliate { Id = 1, Name = "Test Affiliate", ExternalId = "TEST001", IsActive = true };
        var applicant = new Applicant
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            SsnHash = "hashedssn",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "555-1234",
            Email = "john@example.com",
            Address = new Address { Street = "123 Main St", City = "Anytown", State = "CA", ZipCode = "12345" }
        };
        var user = new User
        {
            Id = 1,
            FirstName = "Underwriter",
            LastName = "User",
            UserName = "underwriter@test.com",
            Email = "underwriter@test.com"
        };
        var application = new LoanApplication
        {
            Id = 1,
            AffiliateId = 1,
            ApplicantId = 1,
            ProductType = "Personal Loan",
            Amount = 10000,
            IncomeMonthly = 5000,
            EmploymentType = "Full-time",
            CreditScore = 720,
            Status = ApplicationStatus.InReview,
            Affiliate = affiliate,
            Applicant = applicant
        };

        _context.Affiliates.Add(affiliate);
        _context.Applicants.Add(applicant);
        _context.Users.Add(user);
        _context.LoanApplications.Add(application);
        await _context.SaveChangesAsync();

        var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "underwriter@test.com"),
            new Claim(ClaimTypes.Role, "Underwriter")
        }));

        var request = new ManualDecisionRequest
        {
            Outcome = DecisionOutcome.Approve,
            Reasons = new[] { "Manual approval after review" },
            Justification = "Customer has strong payment history"
        };

        _mockCurrentUserService.Setup(x => x.GetUserId()).Returns(1);

        // Act
        var result = await _decisionService.MakeManualDecisionAsync(1, request, userClaims);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DecisionOutcome.Approve, result.Outcome);
        Assert.Equal(0, result.Score); // Manual decisions have score 0
        Assert.Single(result.Reasons);
        Assert.Contains("Manual approval after review", result.Reasons);
        Assert.Equal(1, result.DecidedByUserId);
        Assert.False(result.IsAutomated);

        // Verify application status was updated
        var updatedApplication = await _context.LoanApplications.FindAsync(1);
        Assert.Equal(ApplicationStatus.Approved, updatedApplication!.Status);

        // Verify decision was saved
        var savedDecision = await _context.Decisions.FirstOrDefaultAsync(d => d.LoanApplicationId == 1);
        Assert.NotNull(savedDecision);
        Assert.Equal(DecisionOutcome.Approve, savedDecision.Outcome);
        Assert.Equal(1, savedDecision.DecidedByUserId);
    }

    [Fact]
    public async Task MakeManualDecisionAsync_WithAlreadyFinalizedApplication_ThrowsInvalidOperationException()
    {
        // Arrange
        var affiliate = new Affiliate { Id = 1, Name = "Test Affiliate", ExternalId = "TEST001", IsActive = true };
        var applicant = new Applicant
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            SsnHash = "hashedssn",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "555-1234",
            Email = "john@example.com",
            Address = new Address { Street = "123 Main St", City = "Anytown", State = "CA", ZipCode = "12345" }
        };
        var application = new LoanApplication
        {
            Id = 1,
            AffiliateId = 1,
            ApplicantId = 1,
            ProductType = "Personal Loan",
            Amount = 10000,
            IncomeMonthly = 5000,
            EmploymentType = "Full-time",
            CreditScore = 720,
            Status = ApplicationStatus.Approved, // Already finalized
            Affiliate = affiliate,
            Applicant = applicant
        };

        _context.Affiliates.Add(affiliate);
        _context.Applicants.Add(applicant);
        _context.LoanApplications.Add(application);
        await _context.SaveChangesAsync();

        var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Underwriter")
        }));

        var request = new ManualDecisionRequest
        {
            Outcome = DecisionOutcome.Reject,
            Reasons = new[] { "Changed mind" }
        };



        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _decisionService.MakeManualDecisionAsync(1, request, userClaims));

        Assert.Contains("already in final status Approved", exception.Message);
    }

    [Fact]
    public async Task GetLatestDecisionAsync_WithExistingDecisions_ReturnsLatestDecision()
    {
        // Arrange
        var affiliate = new Affiliate { Id = 1, Name = "Test Affiliate", ExternalId = "TEST001", IsActive = true };
        var applicant = new Applicant
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            SsnHash = "hashedssn",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "555-1234",
            Email = "john@example.com",
            Address = new Address { Street = "123 Main St", City = "Anytown", State = "CA", ZipCode = "12345" }
        };
        var user = new User
        {
            Id = 1,
            FirstName = "Underwriter",
            LastName = "User",
            UserName = "underwriter@test.com",
            Email = "underwriter@test.com"
        };
        var application = new LoanApplication
        {
            Id = 1,
            AffiliateId = 1,
            ApplicantId = 1,
            ProductType = "Personal Loan",
            Amount = 10000,
            IncomeMonthly = 5000,
            EmploymentType = "Full-time",
            CreditScore = 720,
            Status = ApplicationStatus.Approved,
            Affiliate = affiliate,
            Applicant = applicant
        };

        var decision1 = new Decision
        {
            Id = 1,
            LoanApplicationId = 1,
            Outcome = DecisionOutcome.ManualReview,
            Score = 600,
            Reasons = new[] { "Needs review" },
            DecidedAt = DateTime.UtcNow.AddHours(-2)
        };

        var decision2 = new Decision
        {
            Id = 2,
            LoanApplicationId = 1,
            Outcome = DecisionOutcome.Approve,
            Score = 0,
            Reasons = new[] { "Manual approval" },
            DecidedByUserId = 1,
            DecidedAt = DateTime.UtcNow.AddHours(-1)
        };

        _context.Affiliates.Add(affiliate);
        _context.Applicants.Add(applicant);
        _context.Users.Add(user);
        _context.LoanApplications.Add(application);
        _context.Decisions.AddRange(decision1, decision2);
        await _context.SaveChangesAsync();

        var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Admin")
        }));

        // Act
        var result = await _decisionService.GetLatestDecisionAsync(1, userClaims);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal(DecisionOutcome.Approve, result.Outcome);
        Assert.Equal(1, result.DecidedByUserId);
        Assert.False(result.IsAutomated);
    }

    [Fact]
    public async Task GetDecisionHistoryAsync_WithMultipleDecisions_ReturnsAllDecisionsOrderedByDate()
    {
        // Arrange
        var affiliate = new Affiliate { Id = 1, Name = "Test Affiliate", ExternalId = "TEST001", IsActive = true };
        var applicant = new Applicant
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            SsnHash = "hashedssn",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "555-1234",
            Email = "john@example.com",
            Address = new Address { Street = "123 Main St", City = "Anytown", State = "CA", ZipCode = "12345" }
        };
        var application = new LoanApplication
        {
            Id = 1,
            AffiliateId = 1,
            ApplicantId = 1,
            ProductType = "Personal Loan",
            Amount = 10000,
            IncomeMonthly = 5000,
            EmploymentType = "Full-time",
            CreditScore = 720,
            Status = ApplicationStatus.Approved,
            Affiliate = affiliate,
            Applicant = applicant
        };

        var decision1 = new Decision
        {
            Id = 1,
            LoanApplicationId = 1,
            Outcome = DecisionOutcome.ManualReview,
            Score = 600,
            Reasons = new[] { "Needs review" },
            DecidedAt = DateTime.UtcNow.AddHours(-2)
        };

        var decision2 = new Decision
        {
            Id = 2,
            LoanApplicationId = 1,
            Outcome = DecisionOutcome.Approve,
            Score = 0,
            Reasons = new[] { "Manual approval" },
            DecidedByUserId = 1,
            DecidedAt = DateTime.UtcNow.AddHours(-1)
        };

        _context.Affiliates.Add(affiliate);
        _context.Applicants.Add(applicant);
        _context.LoanApplications.Add(application);
        _context.Decisions.AddRange(decision1, decision2);
        await _context.SaveChangesAsync();

        var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Admin")
        }));

        // Act
        var result = await _decisionService.GetDecisionHistoryAsync(1, userClaims);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].Id); // Latest decision first
        Assert.Equal(1, result[1].Id);
    }

    [Fact]
    public async Task MakeManualDecisionAsync_WithRejectOutcome_UpdatesStatusToRejected()
    {
        // Arrange
        var affiliate = new Affiliate { Id = 1, Name = "Test Affiliate", ExternalId = "TEST001", IsActive = true };
        var applicant = new Applicant
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            SsnHash = "hashedssn",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "555-1234",
            Email = "john@example.com",
            Address = new Address { Street = "123 Main St", City = "Anytown", State = "CA", ZipCode = "12345" }
        };
        var user = new User
        {
            Id = 1,
            FirstName = "Underwriter",
            LastName = "User",
            UserName = "underwriter@test.com",
            Email = "underwriter@test.com"
        };
        var application = new LoanApplication
        {
            Id = 1,
            AffiliateId = 1,
            ApplicantId = 1,
            ProductType = "Personal Loan",
            Amount = 10000,
            IncomeMonthly = 5000,
            EmploymentType = "Full-time",
            CreditScore = 720,
            Status = ApplicationStatus.InReview,
            Affiliate = affiliate,
            Applicant = applicant
        };

        _context.Affiliates.Add(affiliate);
        _context.Applicants.Add(applicant);
        _context.Users.Add(user);
        _context.LoanApplications.Add(application);
        await _context.SaveChangesAsync();

        var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "underwriter@test.com"),
            new Claim(ClaimTypes.Role, "Underwriter")
        }));

        var request = new ManualDecisionRequest
        {
            Outcome = DecisionOutcome.Reject,
            Reasons = new[] { "Insufficient income verification" },
            Justification = "Unable to verify employment"
        };

        _mockCurrentUserService.Setup(x => x.GetUserId()).Returns(1);

        // Act
        var result = await _decisionService.MakeManualDecisionAsync(1, request, userClaims);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DecisionOutcome.Reject, result.Outcome);
        Assert.Equal(1, result.DecidedByUserId);
        Assert.False(result.IsAutomated);

        // Verify application status was updated
        var updatedApplication = await _context.LoanApplications.FindAsync(1);
        Assert.Equal(ApplicationStatus.Rejected, updatedApplication!.Status);
    }

    [Fact]
    public async Task MakeManualDecisionAsync_WithAffiliateUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var affiliate = new Affiliate { Id = 1, Name = "Test Affiliate", ExternalId = "TEST001", IsActive = true };
        var applicant = new Applicant
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            SsnHash = "hashedssn",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "555-1234",
            Email = "john@example.com",
            Address = new Address { Street = "123 Main St", City = "Anytown", State = "CA", ZipCode = "12345" }
        };
        var application = new LoanApplication
        {
            Id = 1,
            AffiliateId = 1,
            ApplicantId = 1,
            ProductType = "Personal Loan",
            Amount = 10000,
            IncomeMonthly = 5000,
            EmploymentType = "Full-time",
            CreditScore = 720,
            Status = ApplicationStatus.InReview,
            Affiliate = affiliate,
            Applicant = applicant
        };

        _context.Affiliates.Add(affiliate);
        _context.Applicants.Add(applicant);
        _context.LoanApplications.Add(application);
        await _context.SaveChangesAsync();

        var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Affiliate")
        }));

        var request = new ManualDecisionRequest
        {
            Outcome = DecisionOutcome.Approve,
            Reasons = new[] { "Looks good to me" }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _decisionService.MakeManualDecisionAsync(1, request, userClaims));

        Assert.Contains("does not have permission to make decisions", exception.Message);
    }

    [Fact]
    public async Task GetLatestDecisionAsync_WithAffiliateUserAccessingOwnApplication_ReturnsDecision()
    {
        // Arrange
        var affiliate = new Affiliate { Id = 1, Name = "Test Affiliate", ExternalId = "TEST001", IsActive = true };
        var applicant = new Applicant
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            SsnHash = "hashedssn",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "555-1234",
            Email = "john@example.com",
            Address = new Address { Street = "123 Main St", City = "Anytown", State = "CA", ZipCode = "12345" }
        };
        var user = new User
        {
            Id = 1,
            FirstName = "Affiliate",
            LastName = "User",
            UserName = "affiliate@test.com",
            Email = "affiliate@test.com",
            AffiliateId = 1
        };
        var application = new LoanApplication
        {
            Id = 1,
            AffiliateId = 1,
            ApplicantId = 1,
            ProductType = "Personal Loan",
            Amount = 10000,
            IncomeMonthly = 5000,
            EmploymentType = "Full-time",
            CreditScore = 720,
            Status = ApplicationStatus.Approved,
            Affiliate = affiliate,
            Applicant = applicant
        };

        var decision = new Decision
        {
            Id = 1,
            LoanApplicationId = 1,
            Outcome = DecisionOutcome.Approve,
            Score = 750,
            Reasons = new[] { "Good credit score" },
            DecidedAt = DateTime.UtcNow
        };

        _context.Affiliates.Add(affiliate);
        _context.Applicants.Add(applicant);
        _context.Users.Add(user);
        _context.LoanApplications.Add(application);
        _context.Decisions.Add(decision);
        await _context.SaveChangesAsync();

        var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Affiliate")
        }));

        _mockCurrentUserService.Setup(x => x.GetUserId()).Returns(1);

        // Act
        var result = await _decisionService.GetLatestDecisionAsync(1, userClaims);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DecisionOutcome.Approve, result.Outcome);
        Assert.Equal(1, result.LoanApplicationId);
    }

    [Fact]
    public async Task GetLatestDecisionAsync_WithAffiliateUserAccessingOtherApplication_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var affiliate1 = new Affiliate { Id = 1, Name = "Test Affiliate 1", ExternalId = "TEST001", IsActive = true };
        var affiliate2 = new Affiliate { Id = 2, Name = "Test Affiliate 2", ExternalId = "TEST002", IsActive = true };
        var applicant = new Applicant
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            SsnHash = "hashedssn",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "555-1234",
            Email = "john@example.com",
            Address = new Address { Street = "123 Main St", City = "Anytown", State = "CA", ZipCode = "12345" }
        };
        var user = new User
        {
            Id = 1,
            FirstName = "Affiliate",
            LastName = "User",
            UserName = "affiliate@test.com",
            Email = "affiliate@test.com",
            AffiliateId = 1 // User belongs to affiliate 1
        };
        var application = new LoanApplication
        {
            Id = 1,
            AffiliateId = 2, // Application belongs to affiliate 2
            ApplicantId = 1,
            ProductType = "Personal Loan",
            Amount = 10000,
            IncomeMonthly = 5000,
            EmploymentType = "Full-time",
            CreditScore = 720,
            Status = ApplicationStatus.Approved,
            Affiliate = affiliate2,
            Applicant = applicant
        };

        _context.Affiliates.AddRange(affiliate1, affiliate2);
        _context.Applicants.Add(applicant);
        _context.Users.Add(user);
        _context.LoanApplications.Add(application);
        await _context.SaveChangesAsync();

        var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Affiliate")
        }));

        _mockCurrentUserService.Setup(x => x.GetUserId()).Returns(1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _decisionService.GetLatestDecisionAsync(1, userClaims));

        Assert.Contains("does not have permission to view this application", exception.Message);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}