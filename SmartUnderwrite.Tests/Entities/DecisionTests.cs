using FluentAssertions;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Core.Enums;
using Xunit;

namespace SmartUnderwrite.Tests.Entities;

public class DecisionTests
{
    [Fact]
    public void Decision_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var decision = new Decision();

        // Assert
        decision.Id.Should().Be(0);
        decision.LoanApplicationId.Should().Be(0);
        decision.Outcome.Should().Be(DecisionOutcome.Approve); // Default enum value
        decision.Score.Should().Be(0);
        decision.Reasons.Should().NotBeNull().And.BeEmpty();
        decision.DecidedByUserId.Should().BeNull();
        decision.DecidedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        decision.LoanApplication.Should().BeNull();
        decision.DecidedByUser.Should().BeNull();
    }

    [Fact]
    public void Decision_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var decidedAt = DateTime.UtcNow.AddMinutes(-30);
        var reasons = new[] { "Good credit score", "Stable income" };

        // Act
        var decision = new Decision
        {
            Id = 1,
            LoanApplicationId = 100,
            Outcome = DecisionOutcome.Approve,
            Score = 750,
            Reasons = reasons,
            DecidedByUserId = 200,
            DecidedAt = decidedAt
        };

        // Assert
        decision.Id.Should().Be(1);
        decision.LoanApplicationId.Should().Be(100);
        decision.Outcome.Should().Be(DecisionOutcome.Approve);
        decision.Score.Should().Be(750);
        decision.Reasons.Should().BeEquivalentTo(reasons);
        decision.DecidedByUserId.Should().Be(200);
        decision.DecidedAt.Should().Be(decidedAt);
    }

    [Theory]
    [InlineData(DecisionOutcome.Approve)]
    [InlineData(DecisionOutcome.Reject)]
    [InlineData(DecisionOutcome.ManualReview)]
    public void Decision_WithDifferentOutcomes_ShouldSetCorrectly(DecisionOutcome outcome)
    {
        // Act
        var decision = new Decision
        {
            Outcome = outcome
        };

        // Assert
        decision.Outcome.Should().Be(outcome);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(300)]
    [InlineData(850)]
    [InlineData(-100)] // Edge case - negative score
    public void Decision_WithVariousScores_ShouldAcceptAllValues(int score)
    {
        // Act
        var decision = new Decision
        {
            Score = score
        };

        // Assert
        decision.Score.Should().Be(score);
    }

    [Fact]
    public void Decision_WithMultipleReasons_ShouldStoreAllReasons()
    {
        // Arrange
        var reasons = new[]
        {
            "Credit score above threshold",
            "Income meets requirements",
            "Employment verified",
            "Debt-to-income ratio acceptable"
        };

        // Act
        var decision = new Decision
        {
            Reasons = reasons
        };

        // Assert
        decision.Reasons.Should().HaveCount(4);
        decision.Reasons.Should().BeEquivalentTo(reasons);
        decision.Reasons.Should().Contain("Credit score above threshold");
        decision.Reasons.Should().Contain("Income meets requirements");
        decision.Reasons.Should().Contain("Employment verified");
        decision.Reasons.Should().Contain("Debt-to-income ratio acceptable");
    }

    [Fact]
    public void Decision_WithEmptyReasons_ShouldAllowEmptyArray()
    {
        // Act
        var decision = new Decision
        {
            Reasons = Array.Empty<string>()
        };

        // Assert
        decision.Reasons.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Decision_WithNullDecidedByUserId_ShouldAllowNull()
    {
        // Act
        var decision = new Decision
        {
            DecidedByUserId = null
        };

        // Assert
        decision.DecidedByUserId.Should().BeNull();
    }

    [Fact]
    public void Decision_NavigationProperties_ShouldInitializeCorrectly()
    {
        // Act
        var decision = new Decision();

        // Assert
        decision.LoanApplication.Should().BeNull();
        decision.DecidedByUser.Should().BeNull();
    }

    [Fact]
    public void Decision_AutomatedDecision_ShouldHaveNullUser()
    {
        // Act
        var decision = new Decision
        {
            Outcome = DecisionOutcome.Approve,
            Score = 700,
            Reasons = new[] { "Automated approval based on rules" },
            DecidedByUserId = null // Automated decision
        };

        // Assert
        decision.DecidedByUserId.Should().BeNull();
        decision.DecidedByUser.Should().BeNull();
        decision.Reasons.Should().Contain("Automated approval based on rules");
    }

    [Fact]
    public void Decision_ManualDecision_ShouldHaveUserId()
    {
        // Act
        var decision = new Decision
        {
            Outcome = DecisionOutcome.Reject,
            Score = 450,
            Reasons = new[] { "Manual review - insufficient documentation" },
            DecidedByUserId = 123 // Manual decision by user
        };

        // Assert
        decision.DecidedByUserId.Should().Be(123);
        decision.Reasons.Should().Contain("Manual review - insufficient documentation");
    }

    [Fact]
    public void Decision_WithSpecialCharactersInReasons_ShouldHandleCorrectly()
    {
        // Arrange
        var reasons = new[]
        {
            "Credit score: 750+ (excellent)",
            "Income/Expense ratio: 3.5:1",
            "Employment: \"Software Engineer\" @ TechCorp",
            "Notes: Applicant has 10% down payment available"
        };

        // Act
        var decision = new Decision
        {
            Reasons = reasons
        };

        // Assert
        decision.Reasons.Should().BeEquivalentTo(reasons);
        decision.Reasons.Should().Contain("Credit score: 750+ (excellent)");
        decision.Reasons.Should().Contain("Income/Expense ratio: 3.5:1");
        decision.Reasons.Should().Contain("Employment: \"Software Engineer\" @ TechCorp");
        decision.Reasons.Should().Contain("Notes: Applicant has 10% down payment available");
    }
}