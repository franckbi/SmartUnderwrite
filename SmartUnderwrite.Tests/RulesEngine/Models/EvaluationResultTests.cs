using FluentAssertions;
using SmartUnderwrite.Core.Enums;
using SmartUnderwrite.Core.RulesEngine.Models;
using Xunit;

namespace SmartUnderwrite.Tests.RulesEngine.Models;

public class EvaluationResultTests
{
    [Fact]
    public void EvaluationResult_DefaultConstructor_ShouldInitializeCollections()
    {
        // Act
        var result = new EvaluationResult();

        // Assert
        result.Outcome.Should().Be(DecisionOutcome.Approve); // Default enum value
        result.Score.Should().Be(0);
        result.Reasons.Should().NotBeNull().And.BeEmpty();
        result.RuleResults.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void EvaluationResult_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var reasons = new List<string> { "Good credit score", "Stable income" };
        var ruleResults = new List<RuleExecutionResult>
        {
            new() { RuleName = "Credit Rule", Executed = true, Outcome = DecisionOutcome.Approve }
        };

        // Act
        var result = new EvaluationResult
        {
            Outcome = DecisionOutcome.Approve,
            Score = 750,
            Reasons = reasons,
            RuleResults = ruleResults
        };

        // Assert
        result.Outcome.Should().Be(DecisionOutcome.Approve);
        result.Score.Should().Be(750);
        result.Reasons.Should().BeEquivalentTo(reasons);
        result.RuleResults.Should().BeEquivalentTo(ruleResults);
    }

    [Theory]
    [InlineData(DecisionOutcome.Approve)]
    [InlineData(DecisionOutcome.Reject)]
    [InlineData(DecisionOutcome.ManualReview)]
    public void EvaluationResult_WithDifferentOutcomes_ShouldSetCorrectly(DecisionOutcome outcome)
    {
        // Act
        var result = new EvaluationResult
        {
            Outcome = outcome
        };

        // Assert
        result.Outcome.Should().Be(outcome);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(300)]
    [InlineData(850)]
    [InlineData(-100)]
    public void EvaluationResult_WithVariousScores_ShouldAcceptAllValues(int score)
    {
        // Act
        var result = new EvaluationResult
        {
            Score = score
        };

        // Assert
        result.Score.Should().Be(score);
    }

    [Fact]
    public void EvaluationResult_AddReason_ShouldAddToCollection()
    {
        // Arrange
        var result = new EvaluationResult();

        // Act
        result.Reasons.Add("Test reason");

        // Assert
        result.Reasons.Should().HaveCount(1);
        result.Reasons.Should().Contain("Test reason");
    }

    [Fact]
    public void EvaluationResult_AddRuleResult_ShouldAddToCollection()
    {
        // Arrange
        var result = new EvaluationResult();
        var ruleResult = new RuleExecutionResult
        {
            RuleName = "Test Rule",
            Executed = true
        };

        // Act
        result.RuleResults.Add(ruleResult);

        // Assert
        result.RuleResults.Should().HaveCount(1);
        result.RuleResults.Should().Contain(ruleResult);
    }
}

public class RuleExecutionResultTests
{
    [Fact]
    public void RuleExecutionResult_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var result = new RuleExecutionResult();

        // Assert
        result.RuleName.Should().Be(string.Empty);
        result.Executed.Should().BeFalse();
        result.Outcome.Should().BeNull();
        result.Reason.Should().BeNull();
        result.ScoreImpact.Should().Be(0);
        result.Errors.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void RuleExecutionResult_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var errors = new List<string> { "Invalid condition", "Missing property" };

        // Act
        var result = new RuleExecutionResult
        {
            RuleName = "Credit Score Rule",
            Executed = true,
            Outcome = DecisionOutcome.Approve,
            Reason = "Credit score above threshold",
            ScoreImpact = 50,
            Errors = errors
        };

        // Assert
        result.RuleName.Should().Be("Credit Score Rule");
        result.Executed.Should().BeTrue();
        result.Outcome.Should().Be(DecisionOutcome.Approve);
        result.Reason.Should().Be("Credit score above threshold");
        result.ScoreImpact.Should().Be(50);
        result.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void RuleExecutionResult_WithNullOutcome_ShouldAllowNull()
    {
        // Act
        var result = new RuleExecutionResult
        {
            Outcome = null
        };

        // Assert
        result.Outcome.Should().BeNull();
    }

    [Fact]
    public void RuleExecutionResult_WithNullReason_ShouldAllowNull()
    {
        // Act
        var result = new RuleExecutionResult
        {
            Reason = null
        };

        // Assert
        result.Reason.Should().BeNull();
    }

    [Theory]
    [InlineData(-100)]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void RuleExecutionResult_WithVariousScoreImpacts_ShouldAcceptAllValues(int scoreImpact)
    {
        // Act
        var result = new RuleExecutionResult
        {
            ScoreImpact = scoreImpact
        };

        // Assert
        result.ScoreImpact.Should().Be(scoreImpact);
    }

    [Fact]
    public void RuleExecutionResult_AddError_ShouldAddToCollection()
    {
        // Arrange
        var result = new RuleExecutionResult();

        // Act
        result.Errors.Add("Test error");

        // Assert
        result.Errors.Should().HaveCount(1);
        result.Errors.Should().Contain("Test error");
    }

    [Fact]
    public void RuleExecutionResult_SuccessfulExecution_ShouldHaveNoErrors()
    {
        // Act
        var result = new RuleExecutionResult
        {
            RuleName = "Success Rule",
            Executed = true,
            Outcome = DecisionOutcome.Approve,
            Reason = "All conditions met"
        };

        // Assert
        result.Executed.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Outcome.Should().NotBeNull();
        result.Reason.Should().NotBeNull();
    }

    [Fact]
    public void RuleExecutionResult_FailedExecution_ShouldHaveErrors()
    {
        // Act
        var result = new RuleExecutionResult
        {
            RuleName = "Failed Rule",
            Executed = false,
            Errors = new List<string> { "Compilation error", "Invalid syntax" }
        };

        // Assert
        result.Executed.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Compilation error");
        result.Errors.Should().Contain("Invalid syntax");
        result.Outcome.Should().BeNull();
    }

    [Fact]
    public void RuleExecutionResult_WithComplexRuleName_ShouldHandleCorrectly()
    {
        // Act
        var result = new RuleExecutionResult
        {
            RuleName = "Complex Rule: Credit Score >= 700 AND Income > $5000"
        };

        // Assert
        result.RuleName.Should().Be("Complex Rule: Credit Score >= 700 AND Income > $5000");
    }

    [Fact]
    public void RuleExecutionResult_WithComplexReason_ShouldHandleCorrectly()
    {
        // Act
        var result = new RuleExecutionResult
        {
            Reason = "Applicant meets all criteria: Credit Score (750) >= 700, Monthly Income ($6,500) > $5,000, Employment Type = 'Full-Time'"
        };

        // Assert
        result.Reason.Should().Be("Applicant meets all criteria: Credit Score (750) >= 700, Monthly Income ($6,500) > $5,000, Employment Type = 'Full-Time'");
    }
}