using SmartUnderwrite.Core.RulesEngine.Compilation;
using SmartUnderwrite.Core.RulesEngine.Parsing;
using SmartUnderwrite.Core.RulesEngine.Models;
using Xunit;
using FluentAssertions;
using System.Text.Json;

namespace SmartUnderwrite.Tests.RulesEngine;

public class RuleParserTests
{
    private readonly RuleParser _parser;

    public RuleParserTests()
    {
        var expressionCompiler = new ExpressionCompiler();
        _parser = new RuleParser(expressionCompiler);
    }

    [Fact]
    public void ParseRuleDefinition_ValidJson_ShouldParseSuccessfully()
    {
        // Arrange
        var ruleJson = """
        {
            "name": "Basic Credit Check",
            "priority": 10,
            "clauses": [
                {
                    "if": "CreditScore < 550",
                    "then": "REJECT",
                    "reason": "Low credit score"
                }
            ],
            "score": {
                "base": 600,
                "add": [
                    {
                        "when": "CreditScore >= 720",
                        "points": 50
                    }
                ]
            }
        }
        """;

        // Act
        var result = _parser.ParseRuleDefinition(ruleJson);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Basic Credit Check");
        result.Priority.Should().Be(10);
        result.Clauses.Should().HaveCount(1);
        result.Clauses[0].Condition.Should().Be("CreditScore < 550");
        result.Clauses[0].Action.Should().Be("REJECT");
        result.Clauses[0].Reason.Should().Be("Low credit score");
        result.Score.Should().NotBeNull();
        result.Score.Base.Should().Be(600);
        result.Score.Add.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ParseRuleDefinition_EmptyOrNullJson_ShouldThrowArgumentException(string ruleJson)
    {
        // Act & Assert
        _parser.Invoking(p => p.ParseRuleDefinition(ruleJson))
            .Should().Throw<ArgumentException>()
            .WithMessage("*cannot be null or empty*");
    }

    [Fact]
    public void ParseRuleDefinition_InvalidJson_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act & Assert
        _parser.Invoking(p => p.ParseRuleDefinition(invalidJson))
            .Should().Throw<ArgumentException>()
            .WithMessage("*Invalid JSON format*");
    }

    [Fact]
    public void ValidateRuleDefinition_ValidRule_ShouldReturnSuccess()
    {
        // Arrange
        var ruleDefinition = new RuleDefinition
        {
            Name = "Test Rule",
            Priority = 10,
            Clauses = new List<RuleClause>
            {
                new RuleClause
                {
                    Condition = "CreditScore > 600",
                    Action = "APPROVE",
                    Reason = "Good credit score"
                }
            }
        };

        // Act
        var result = _parser.ValidateRuleDefinition(ruleDefinition);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRuleDefinition_NullRule_ShouldReturnError()
    {
        // Act
        var result = _parser.ValidateRuleDefinition(null);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Rule definition cannot be null");
    }

    [Fact]
    public void ValidateRuleDefinition_EmptyName_ShouldReturnError()
    {
        // Arrange
        var ruleDefinition = new RuleDefinition
        {
            Name = "",
            Priority = 10,
            Clauses = new List<RuleClause>
            {
                new RuleClause
                {
                    Condition = "CreditScore > 600",
                    Action = "APPROVE",
                    Reason = "Good credit score"
                }
            }
        };

        // Act
        var result = _parser.ValidateRuleDefinition(ruleDefinition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Rule name is required");
    }

    [Fact]
    public void ValidateRuleDefinition_NegativePriority_ShouldReturnError()
    {
        // Arrange
        var ruleDefinition = new RuleDefinition
        {
            Name = "Test Rule",
            Priority = -1,
            Clauses = new List<RuleClause>
            {
                new RuleClause
                {
                    Condition = "CreditScore > 600",
                    Action = "APPROVE",
                    Reason = "Good credit score"
                }
            }
        };

        // Act
        var result = _parser.ValidateRuleDefinition(ruleDefinition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Rule priority must be non-negative");
    }

    [Fact]
    public void ValidateRuleDefinition_NoClauses_ShouldReturnError()
    {
        // Arrange
        var ruleDefinition = new RuleDefinition
        {
            Name = "Test Rule",
            Priority = 10,
            Clauses = new List<RuleClause>()
        };

        // Act
        var result = _parser.ValidateRuleDefinition(ruleDefinition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Rule must have at least one clause");
    }

    [Fact]
    public void ValidateRuleDefinition_InvalidCondition_ShouldReturnError()
    {
        // Arrange
        var ruleDefinition = new RuleDefinition
        {
            Name = "Test Rule",
            Priority = 10,
            Clauses = new List<RuleClause>
            {
                new RuleClause
                {
                    Condition = "InvalidProperty > 600",
                    Action = "APPROVE",
                    Reason = "Test reason"
                }
            }
        };

        // Act
        var result = _parser.ValidateRuleDefinition(ruleDefinition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid condition syntax"));
    }

    [Fact]
    public void ValidateRuleDefinition_InvalidAction_ShouldReturnError()
    {
        // Arrange
        var ruleDefinition = new RuleDefinition
        {
            Name = "Test Rule",
            Priority = 10,
            Clauses = new List<RuleClause>
            {
                new RuleClause
                {
                    Condition = "CreditScore > 600",
                    Action = "INVALID_ACTION",
                    Reason = "Test reason"
                }
            }
        };

        // Act
        var result = _parser.ValidateRuleDefinition(ruleDefinition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid action"));
    }

    [Fact]
    public void ValidateRuleDefinition_EmptyReason_ShouldReturnWarning()
    {
        // Arrange
        var ruleDefinition = new RuleDefinition
        {
            Name = "Test Rule",
            Priority = 10,
            Clauses = new List<RuleClause>
            {
                new RuleClause
                {
                    Condition = "CreditScore > 600",
                    Action = "APPROVE",
                    Reason = ""
                }
            }
        };

        // Act
        var result = _parser.ValidateRuleDefinition(ruleDefinition);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("Reason is recommended"));
    }

    [Fact]
    public void ValidateRuleDefinition_NegativeBaseScore_ShouldReturnError()
    {
        // Arrange
        var ruleDefinition = new RuleDefinition
        {
            Name = "Test Rule",
            Priority = 10,
            Clauses = new List<RuleClause>
            {
                new RuleClause
                {
                    Condition = "CreditScore > 600",
                    Action = "APPROVE",
                    Reason = "Good credit"
                }
            },
            Score = new ScoreDefinition
            {
                Base = -100
            }
        };

        // Act
        var result = _parser.ValidateRuleDefinition(ruleDefinition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Base score must be non-negative");
    }

    [Fact]
    public void ValidateRuleDefinition_InvalidScoreModifierCondition_ShouldReturnError()
    {
        // Arrange
        var ruleDefinition = new RuleDefinition
        {
            Name = "Test Rule",
            Priority = 10,
            Clauses = new List<RuleClause>
            {
                new RuleClause
                {
                    Condition = "CreditScore > 600",
                    Action = "APPROVE",
                    Reason = "Good credit"
                }
            },
            Score = new ScoreDefinition
            {
                Base = 600,
                Add = new List<ScoreModifier>
                {
                    new ScoreModifier
                    {
                        Condition = "InvalidProperty > 700",
                        Points = 50
                    }
                }
            }
        };

        // Act
        var result = _parser.ValidateRuleDefinition(ruleDefinition);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid condition syntax"));
    }

    [Fact]
    public void ValidateRuleJson_ValidJson_ShouldReturnSuccess()
    {
        // Arrange
        var ruleJson = """
        {
            "name": "Test Rule",
            "priority": 10,
            "clauses": [
                {
                    "if": "CreditScore > 600",
                    "then": "APPROVE",
                    "reason": "Good credit score"
                }
            ]
        }
        """;

        // Act
        var result = _parser.ValidateRuleJson(ruleJson);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRuleJson_InvalidJson_ShouldReturnError()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var result = _parser.ValidateRuleJson(invalidJson);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("JSON parsing error"));
    }

    [Fact]
    public void ValidateRuleDefinition_ComplexValidRule_ShouldReturnSuccess()
    {
        // Arrange
        var ruleDefinition = new RuleDefinition
        {
            Name = "Complex Rule",
            Priority = 5,
            Clauses = new List<RuleClause>
            {
                new RuleClause
                {
                    Condition = "CreditScore < 550",
                    Action = "REJECT",
                    Reason = "Low credit score"
                },
                new RuleClause
                {
                    Condition = "IncomeMonthly <= 0",
                    Action = "MANUAL",
                    Reason = "No income provided"
                },
                new RuleClause
                {
                    Condition = "Amount > 50000 && CreditScore < 680",
                    Action = "MANUAL",
                    Reason = "High amount risk"
                }
            },
            Score = new ScoreDefinition
            {
                Base = 600,
                Add = new List<ScoreModifier>
                {
                    new ScoreModifier
                    {
                        Condition = "CreditScore >= 720",
                        Points = 50
                    },
                    new ScoreModifier
                    {
                        Condition = "IncomeMonthly > 8000",
                        Points = 25
                    }
                },
                Subtract = new List<ScoreModifier>
                {
                    new ScoreModifier
                    {
                        Condition = "CreditScore < 600",
                        Points = 30
                    }
                }
            }
        };

        // Act
        var result = _parser.ValidateRuleDefinition(ruleDefinition);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}