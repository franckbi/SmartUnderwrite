using FluentAssertions;
using SmartUnderwrite.Core.RulesEngine.Models;
using System.Text.Json;
using Xunit;

namespace SmartUnderwrite.Tests.RulesEngine.Models;

public class RuleDefinitionTests
{
    [Fact]
    public void RuleDefinition_DefaultConstructor_ShouldInitializeCollections()
    {
        // Act
        var rule = new RuleDefinition();

        // Assert
        rule.Name.Should().Be(string.Empty);
        rule.Priority.Should().Be(0);
        rule.Clauses.Should().NotBeNull().And.BeEmpty();
        rule.Score.Should().BeNull();
    }

    [Fact]
    public void RuleDefinition_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var clauses = new List<RuleClause>
        {
            new() { Condition = "CreditScore > 700", Action = "APPROVE", Reason = "Good credit" }
        };
        var score = new ScoreDefinition { Base = 600 };

        // Act
        var rule = new RuleDefinition
        {
            Name = "Credit Rule",
            Priority = 10,
            Clauses = clauses,
            Score = score
        };

        // Assert
        rule.Name.Should().Be("Credit Rule");
        rule.Priority.Should().Be(10);
        rule.Clauses.Should().BeEquivalentTo(clauses);
        rule.Score.Should().Be(score);
    }

    [Fact]
    public void RuleDefinition_JsonSerialization_ShouldSerializeCorrectly()
    {
        // Arrange
        var rule = new RuleDefinition
        {
            Name = "Test Rule",
            Priority = 5,
            Clauses = new List<RuleClause>
            {
                new() { Condition = "Amount > 1000", Action = "APPROVE", Reason = "High amount" }
            },
            Score = new ScoreDefinition
            {
                Base = 500,
                Add = new List<ScoreModifier>
                {
                    new() { Condition = "CreditScore > 750", Points = 50 }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(rule);
        var deserializedRule = JsonSerializer.Deserialize<RuleDefinition>(json);

        // Assert
        deserializedRule.Should().NotBeNull();
        deserializedRule!.Name.Should().Be("Test Rule");
        deserializedRule.Priority.Should().Be(5);
        deserializedRule.Clauses.Should().HaveCount(1);
        deserializedRule.Score.Should().NotBeNull();
        deserializedRule.Score!.Base.Should().Be(500);
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(0)]
    [InlineData(100)]
    public void RuleDefinition_WithVariousPriorities_ShouldAcceptAllValues(int priority)
    {
        // Act
        var rule = new RuleDefinition
        {
            Priority = priority
        };

        // Assert
        rule.Priority.Should().Be(priority);
    }

    [Fact]
    public void RuleDefinition_AddClause_ShouldAddToCollection()
    {
        // Arrange
        var rule = new RuleDefinition();
        var clause = new RuleClause
        {
            Condition = "Income > 5000",
            Action = "APPROVE",
            Reason = "Sufficient income"
        };

        // Act
        rule.Clauses.Add(clause);

        // Assert
        rule.Clauses.Should().HaveCount(1);
        rule.Clauses.Should().Contain(clause);
    }
}

public class RuleClauseTests
{
    [Fact]
    public void RuleClause_DefaultConstructor_ShouldSetEmptyStrings()
    {
        // Act
        var clause = new RuleClause();

        // Assert
        clause.Condition.Should().Be(string.Empty);
        clause.Action.Should().Be(string.Empty);
        clause.Reason.Should().Be(string.Empty);
    }

    [Fact]
    public void RuleClause_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Act
        var clause = new RuleClause
        {
            Condition = "CreditScore >= 700",
            Action = "APPROVE",
            Reason = "Excellent credit score"
        };

        // Assert
        clause.Condition.Should().Be("CreditScore >= 700");
        clause.Action.Should().Be("APPROVE");
        clause.Reason.Should().Be("Excellent credit score");
    }

    [Theory]
    [InlineData("APPROVE")]
    [InlineData("REJECT")]
    [InlineData("MANUAL")]
    public void RuleClause_WithDifferentActions_ShouldAcceptAllValues(string action)
    {
        // Act
        var clause = new RuleClause
        {
            Action = action
        };

        // Assert
        clause.Action.Should().Be(action);
    }

    [Fact]
    public void RuleClause_WithComplexCondition_ShouldHandleCorrectly()
    {
        // Act
        var clause = new RuleClause
        {
            Condition = "CreditScore >= 700 && IncomeMonthly > 5000 && EmploymentType == \"Full-Time\"",
            Action = "APPROVE",
            Reason = "Meets all approval criteria"
        };

        // Assert
        clause.Condition.Should().Be("CreditScore >= 700 && IncomeMonthly > 5000 && EmploymentType == \"Full-Time\"");
        clause.Action.Should().Be("APPROVE");
        clause.Reason.Should().Be("Meets all approval criteria");
    }

    [Fact]
    public void RuleClause_JsonSerialization_ShouldUseCorrectPropertyNames()
    {
        // Arrange
        var clause = new RuleClause
        {
            Condition = "Amount > 1000",
            Action = "APPROVE",
            Reason = "High amount"
        };

        // Act
        var json = JsonSerializer.Serialize(clause);

        // Assert
        json.Should().Contain("\"if\":");
        json.Should().Contain("\"then\":");
        json.Should().Contain("\"reason\":");
        json.Should().Contain("Amount \\u003E 1000"); // JSON escapes > as \u003E
        json.Should().Contain("APPROVE");
        json.Should().Contain("High amount");
    }
}

public class ScoreDefinitionTests
{
    [Fact]
    public void ScoreDefinition_DefaultConstructor_ShouldInitializeCollections()
    {
        // Act
        var score = new ScoreDefinition();

        // Assert
        score.Base.Should().Be(0);
        score.Add.Should().NotBeNull().And.BeEmpty();
        score.Subtract.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ScoreDefinition_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var addModifiers = new List<ScoreModifier>
        {
            new() { Condition = "CreditScore > 750", Points = 50 }
        };
        var subtractModifiers = new List<ScoreModifier>
        {
            new() { Condition = "IncomeMonthly < 3000", Points = 25 }
        };

        // Act
        var score = new ScoreDefinition
        {
            Base = 600,
            Add = addModifiers,
            Subtract = subtractModifiers
        };

        // Assert
        score.Base.Should().Be(600);
        score.Add.Should().BeEquivalentTo(addModifiers);
        score.Subtract.Should().BeEquivalentTo(subtractModifiers);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(300)]
    [InlineData(850)]
    [InlineData(-100)]
    public void ScoreDefinition_WithVariousBaseScores_ShouldAcceptAllValues(int baseScore)
    {
        // Act
        var score = new ScoreDefinition
        {
            Base = baseScore
        };

        // Assert
        score.Base.Should().Be(baseScore);
    }

    [Fact]
    public void ScoreDefinition_AddModifier_ShouldAddToCollection()
    {
        // Arrange
        var score = new ScoreDefinition();
        var modifier = new ScoreModifier
        {
            Condition = "CreditScore > 700",
            Points = 25
        };

        // Act
        score.Add.Add(modifier);

        // Assert
        score.Add.Should().HaveCount(1);
        score.Add.Should().Contain(modifier);
    }

    [Fact]
    public void ScoreDefinition_SubtractModifier_ShouldAddToCollection()
    {
        // Arrange
        var score = new ScoreDefinition();
        var modifier = new ScoreModifier
        {
            Condition = "CreditScore < 600",
            Points = 50
        };

        // Act
        score.Subtract.Add(modifier);

        // Assert
        score.Subtract.Should().HaveCount(1);
        score.Subtract.Should().Contain(modifier);
    }
}

public class ScoreModifierTests
{
    [Fact]
    public void ScoreModifier_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var modifier = new ScoreModifier();

        // Assert
        modifier.Condition.Should().Be(string.Empty);
        modifier.Points.Should().Be(0);
    }

    [Fact]
    public void ScoreModifier_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Act
        var modifier = new ScoreModifier
        {
            Condition = "CreditScore >= 750",
            Points = 75
        };

        // Assert
        modifier.Condition.Should().Be("CreditScore >= 750");
        modifier.Points.Should().Be(75);
    }

    [Theory]
    [InlineData(-100)]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void ScoreModifier_WithVariousPoints_ShouldAcceptAllValues(int points)
    {
        // Act
        var modifier = new ScoreModifier
        {
            Points = points
        };

        // Assert
        modifier.Points.Should().Be(points);
    }

    [Fact]
    public void ScoreModifier_WithComplexCondition_ShouldHandleCorrectly()
    {
        // Act
        var modifier = new ScoreModifier
        {
            Condition = "CreditScore >= 700 && IncomeMonthly > 5000 && Amount <= 50000",
            Points = 100
        };

        // Assert
        modifier.Condition.Should().Be("CreditScore >= 700 && IncomeMonthly > 5000 && Amount <= 50000");
        modifier.Points.Should().Be(100);
    }

    [Fact]
    public void ScoreModifier_JsonSerialization_ShouldUseCorrectPropertyNames()
    {
        // Arrange
        var modifier = new ScoreModifier
        {
            Condition = "CreditScore > 700",
            Points = 50
        };

        // Act
        var json = JsonSerializer.Serialize(modifier);

        // Assert
        json.Should().Contain("\"when\":");
        json.Should().Contain("\"points\":");
        json.Should().Contain("CreditScore \\u003E 700"); // JSON escapes > as \u003E
        json.Should().Contain("50");
    }
}