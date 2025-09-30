using FluentAssertions;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Core.Enums;
using SmartUnderwrite.Core.ValueObjects;
using Xunit;

namespace SmartUnderwrite.Tests.Entities;

public class LoanApplicationTests
{
    [Fact]
    public void LoanApplication_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var application = new LoanApplication();

        // Assert
        application.Id.Should().Be(0);
        application.ProductType.Should().Be(string.Empty);
        application.EmploymentType.Should().Be(string.Empty);
        application.Status.Should().Be(ApplicationStatus.Submitted);
        application.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        application.UpdatedAt.Should().BeNull();
        application.Documents.Should().NotBeNull().And.BeEmpty();
        application.Decisions.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void LoanApplication_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var updatedAt = DateTime.UtcNow;

        // Act
        var application = new LoanApplication
        {
            Id = 1,
            AffiliateId = 100,
            ApplicantId = 200,
            ProductType = "Personal Loan",
            Amount = 25000m,
            IncomeMonthly = 5000m,
            EmploymentType = "Full-Time",
            CreditScore = 720,
            Status = ApplicationStatus.Evaluated,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Assert
        application.Id.Should().Be(1);
        application.AffiliateId.Should().Be(100);
        application.ApplicantId.Should().Be(200);
        application.ProductType.Should().Be("Personal Loan");
        application.Amount.Should().Be(25000m);
        application.IncomeMonthly.Should().Be(5000m);
        application.EmploymentType.Should().Be("Full-Time");
        application.CreditScore.Should().Be(720);
        application.Status.Should().Be(ApplicationStatus.Evaluated);
        application.CreatedAt.Should().Be(createdAt);
        application.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void LoanApplication_WithNullCreditScore_ShouldAllowNullValue()
    {
        // Act
        var application = new LoanApplication
        {
            CreditScore = null
        };

        // Assert
        application.CreditScore.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    [InlineData(1000000)]
    public void LoanApplication_WithVariousAmounts_ShouldAcceptAllValues(decimal amount)
    {
        // Act
        var application = new LoanApplication
        {
            Amount = amount
        };

        // Assert
        application.Amount.Should().Be(amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    [InlineData(50000)]
    public void LoanApplication_WithVariousIncomes_ShouldAcceptAllValues(decimal income)
    {
        // Act
        var application = new LoanApplication
        {
            IncomeMonthly = income
        };

        // Assert
        application.IncomeMonthly.Should().Be(income);
    }

    [Theory]
    [InlineData(ApplicationStatus.Submitted)]
    [InlineData(ApplicationStatus.Evaluated)]
    [InlineData(ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.ManualReview)]
    public void LoanApplication_WithDifferentStatuses_ShouldSetCorrectly(ApplicationStatus status)
    {
        // Act
        var application = new LoanApplication
        {
            Status = status
        };

        // Assert
        application.Status.Should().Be(status);
    }

    [Fact]
    public void LoanApplication_NavigationProperties_ShouldInitializeCorrectly()
    {
        // Act
        var application = new LoanApplication();

        // Assert
        application.Affiliate.Should().BeNull();
        application.Applicant.Should().BeNull();
        application.Documents.Should().NotBeNull().And.BeEmpty();
        application.Decisions.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void LoanApplication_AddDocument_ShouldAddToCollection()
    {
        // Arrange
        var application = new LoanApplication();
        var document = new Document
        {
            Id = 1,
            FileName = "test.pdf",
            ContentType = "application/pdf"
        };

        // Act
        application.Documents.Add(document);

        // Assert
        application.Documents.Should().HaveCount(1);
        application.Documents.Should().Contain(document);
    }

    [Fact]
    public void LoanApplication_AddDecision_ShouldAddToCollection()
    {
        // Arrange
        var application = new LoanApplication();
        var decision = new Decision
        {
            Id = 1,
            Outcome = DecisionOutcome.Approve,
            Score = 750
        };

        // Act
        application.Decisions.Add(decision);

        // Assert
        application.Decisions.Should().HaveCount(1);
        application.Decisions.Should().Contain(decision);
    }
}