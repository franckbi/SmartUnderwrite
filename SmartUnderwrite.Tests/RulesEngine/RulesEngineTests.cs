using Microsoft.Extensions.Logging;
using Moq;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Core.Enums;
using SmartUnderwrite.Core.RulesEngine.Compilation;
using SmartUnderwrite.Core.RulesEngine.Engine;
using SmartUnderwrite.Core.RulesEngine.Interfaces;
using SmartUnderwrite.Core.RulesEngine.Models;
using SmartUnderwrite.Core.RulesEngine.Parsing;
using SmartUnderwrite.Core.ValueObjects;
using Xunit;
using FluentAssertions;

namespace SmartUnderwrite.Tests.RulesEngine;

public class RulesEngineTests
{
    private readonly Mock<IRuleRepository> _mockRuleRepository;
    private readonly Mock<ILogger<SmartUnderwrite.Core.RulesEngine.Engine.RulesEngine>> _mockLogger;
    private readonly IRuleParser _ruleParser;
    private readonly IExpressionCompiler _expressionCompiler;
    private readonly SmartUnderwrite.Core.RulesEngine.Engine.RulesEngine _rulesEngine;

    public RulesEngineTests()
    {
        _mockRuleRepository = new Mock<IRuleRepository>();
        _mockLogger = new Mock<ILogger<SmartUnderwrite.Core.RulesEngine.Engine.RulesEngine>>();
        _expressionCompiler = new SmartUnderwrite.Core.RulesEngine.Compilation.ExpressionCompiler();
        _ruleParser = new RuleParser(_expressionCompiler);
        
        _rulesEngine = new SmartUnderwrite.Core.RulesEngine.Engine.RulesEngine(
            _ruleParser,
            _expressionCompiler,
            _mockRuleRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task EvaluateAsync_WithApprovalRule_ShouldReturnApprove()
    {
        // Arrange
        var application = CreateTestApplication(creditScore: 750, amount: 25000, incomeMonthly: 6000);
        var applicant = CreateTestApplicant();
        var rules = new List<Rule>
        {
            CreateTestRule("Approval Rule", 10, """
            {
                "name": "Approval Rule",
                "priority": 10,
                "clauses": [
                    {
                        "if": "CreditScore >= 700",
                        "then": "APPROVE",
                        "reason": "Excellent credit score"
                    }
                ]
            }
            """)
        };

        _mockRuleRepository.Setup(r => r.GetActiveRulesAsync())
            .ReturnsAsync(rules);

        // Act
        var result = await _rulesEngine.EvaluateAsync(application, applicant);

        // Assert
        result.Should().NotBeNull();
        result.Outcome.Should().Be(DecisionOutcome.Approve);
        result.Reasons.Should().Contain("Excellent credit score");
        result.RuleResults.Should().HaveCount(1);
        result.RuleResults[0].Executed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_WithRejectionRule_ShouldReturnReject()
    {
        // Arrange
        var application = CreateTestApplication(creditScore: 500, amount: 25000, incomeMonthly: 3000);
        var applicant = CreateTestApplicant();
        var rules = new List<Rule>
        {
            CreateTestRule("Rejection Rule", 10, """
            {
                "name": "Rejection Rule",
                "priority": 10,
                "clauses": [
                    {
                        "if": "CreditScore < 550",
                        "then": "REJECT",
                        "reason": "Low credit score"
                    }
                ]
            }
            """)
        };

        _mockRuleRepository.Setup(r => r.GetActiveRulesAsync())
            .ReturnsAsync(rules);

        // Act
        var result = await _rulesEngine.EvaluateAsync(application, applicant);

        // Assert
        result.Should().NotBeNull();
        result.Outcome.Should().Be(DecisionOutcome.Reject);
        result.Reasons.Should().Contain("Low credit score");
        result.RuleResults.Should().HaveCount(1);
        result.RuleResults[0].Executed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_WithManualReviewRule_ShouldReturnManualReview()
    {
        // Arrange
        var application = CreateTestApplication(creditScore: 650, amount: 75000, incomeMonthly: 5000);
        var applicant = CreateTestApplicant();
        var rules = new List<Rule>
        {
            CreateTestRule("Manual Review Rule", 10, """
            {
                "name": "Manual Review Rule",
                "priority": 10,
                "clauses": [
                    {
                        "if": "Amount > 50000 && CreditScore < 680",
                        "then": "MANUAL",
                        "reason": "High amount with moderate credit"
                    }
                ]
            }
            """)
        };

        _mockRuleRepository.Setup(r => r.GetActiveRulesAsync())
            .ReturnsAsync(rules);

        // Act
        var result = await _rulesEngine.EvaluateAsync(application, applicant);

        // Assert
        result.Should().NotBeNull();
        result.Outcome.Should().Be(DecisionOutcome.ManualReview);
        result.Reasons.Should().Contain("High amount with moderate credit");
    }

    [Fact]
    public async Task EvaluateAsync_WithMultipleRules_ShouldApplyPriorityOrder()
    {
        // Arrange
        var application = CreateTestApplication(creditScore: 600, amount: 30000, incomeMonthly: 4000);
        var applicant = CreateTestApplicant();
        var rules = new List<Rule>
        {
            CreateTestRule("Low Priority Approval", 20, """
            {
                "name": "Low Priority Approval",
                "priority": 20,
                "clauses": [
                    {
                        "if": "CreditScore >= 600",
                        "then": "APPROVE",
                        "reason": "Good credit score"
                    }
                ]
            }
            """),
            CreateTestRule("High Priority Manual", 10, """
            {
                "name": "High Priority Manual",
                "priority": 10,
                "clauses": [
                    {
                        "if": "IncomeMonthly < 5000",
                        "then": "MANUAL",
                        "reason": "Low income requires review"
                    }
                ]
            }
            """)
        };

        _mockRuleRepository.Setup(r => r.GetActiveRulesAsync())
            .ReturnsAsync(rules);

        // Act
        var result = await _rulesEngine.EvaluateAsync(application, applicant);

        // Assert
        result.Should().NotBeNull();
        result.Outcome.Should().Be(DecisionOutcome.ManualReview);
        result.Reasons.Should().Contain("Low income requires review");
        result.Reasons.Should().Contain("Good credit score");
        result.RuleResults.Should().HaveCount(2);
    }

    [Fact]
    public async Task EvaluateAsync_WithConflictingRules_ShouldResolveCorrectly()
    {
        // Arrange
        var application = CreateTestApplication(creditScore: 500, amount: 25000, incomeMonthly: 8000);
        var applicant = CreateTestApplicant();
        var rules = new List<Rule>
        {
            CreateTestRule("Approval Rule", 20, """
            {
                "name": "Approval Rule",
                "priority": 20,
                "clauses": [
                    {
                        "if": "IncomeMonthly > 7000",
                        "then": "APPROVE",
                        "reason": "High income"
                    }
                ]
            }
            """),
            CreateTestRule("Rejection Rule", 10, """
            {
                "name": "Rejection Rule",
                "priority": 10,
                "clauses": [
                    {
                        "if": "CreditScore < 550",
                        "then": "REJECT",
                        "reason": "Low credit score"
                    }
                ]
            }
            """)
        };

        _mockRuleRepository.Setup(r => r.GetActiveRulesAsync())
            .ReturnsAsync(rules);

        // Act
        var result = await _rulesEngine.EvaluateAsync(application, applicant);

        // Assert
        result.Should().NotBeNull();
        result.Outcome.Should().Be(DecisionOutcome.Reject); // Reject should override approve
        result.Reasons.Should().Contain("Low credit score");
        result.Reasons.Should().Contain("High income");
    }

    [Fact]
    public async Task EvaluateAsync_WithScoreCalculation_ShouldCalculateCorrectScore()
    {
        // Arrange
        var application = CreateTestApplication(creditScore: 750, amount: 25000, incomeMonthly: 8000);
        var applicant = CreateTestApplicant();
        var rules = new List<Rule>
        {
            CreateTestRule("Score Rule", 10, """
            {
                "name": "Score Rule",
                "priority": 10,
                "clauses": [
                    {
                        "if": "CreditScore >= 700",
                        "then": "APPROVE",
                        "reason": "Good credit"
                    }
                ],
                "score": {
                    "base": 600,
                    "add": [
                        {
                            "when": "CreditScore >= 750",
                            "points": 50
                        },
                        {
                            "when": "IncomeMonthly > 7000",
                            "points": 25
                        }
                    ]
                }
            }
            """)
        };

        _mockRuleRepository.Setup(r => r.GetActiveRulesAsync())
            .ReturnsAsync(rules);

        // Act
        var result = await _rulesEngine.EvaluateAsync(application, applicant);

        // Assert
        result.Should().NotBeNull();
        result.Score.Should().Be(675); // 600 base + 50 (credit) + 25 (income)
        result.Outcome.Should().Be(DecisionOutcome.Approve);
    }

    [Fact]
    public async Task EvaluateAsync_WithNullCreditScore_ShouldHandleGracefully()
    {
        // Arrange
        var application = CreateTestApplication(creditScore: null, amount: 25000, incomeMonthly: 5000);
        var applicant = CreateTestApplicant();
        var rules = new List<Rule>
        {
            CreateTestRule("Null Credit Rule", 10, """
            {
                "name": "Null Credit Rule",
                "priority": 10,
                "clauses": [
                    {
                        "if": "IncomeMonthly > 4000",
                        "then": "MANUAL",
                        "reason": "No credit score provided"
                    }
                ]
            }
            """)
        };

        _mockRuleRepository.Setup(r => r.GetActiveRulesAsync())
            .ReturnsAsync(rules);

        // Act
        var result = await _rulesEngine.EvaluateAsync(application, applicant);

        // Assert
        result.Should().NotBeNull();
        result.Outcome.Should().Be(DecisionOutcome.ManualReview);
        result.Reasons.Should().Contain("No credit score provided");
    }

    [Fact]
    public async Task EvaluateAsync_WithInvalidRule_ShouldContinueWithOtherRules()
    {
        // Arrange
        var application = CreateTestApplication(creditScore: 700, amount: 25000, incomeMonthly: 5000);
        var applicant = CreateTestApplicant();
        var rules = new List<Rule>
        {
            CreateTestRule("Invalid Rule", 10, """
            {
                "name": "Invalid Rule",
                "priority": 10,
                "clauses": [
                    {
                        "if": "InvalidProperty > 600",
                        "then": "REJECT",
                        "reason": "Invalid condition"
                    }
                ]
            }
            """),
            CreateTestRule("Valid Rule", 20, """
            {
                "name": "Valid Rule",
                "priority": 20,
                "clauses": [
                    {
                        "if": "CreditScore >= 700",
                        "then": "APPROVE",
                        "reason": "Good credit"
                    }
                ]
            }
            """)
        };

        _mockRuleRepository.Setup(r => r.GetActiveRulesAsync())
            .ReturnsAsync(rules);

        // Act
        var result = await _rulesEngine.EvaluateAsync(application, applicant);

        // Assert
        result.Should().NotBeNull();
        result.Outcome.Should().Be(DecisionOutcome.Approve);
        result.Reasons.Should().Contain("Good credit");
        result.RuleResults.Should().HaveCount(2);
        // The invalid rule should not execute successfully but should not prevent other rules from running
        result.RuleResults[0].Executed.Should().BeFalse();
        result.RuleResults[1].Executed.Should().BeTrue();
        result.RuleResults[1].Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateRuleDefinitionAsync_WithValidRule_ShouldReturnTrue()
    {
        // Arrange
        var validRuleJson = """
        {
            "name": "Test Rule",
            "priority": 10,
            "clauses": [
                {
                    "if": "CreditScore > 600",
                    "then": "APPROVE",
                    "reason": "Good credit"
                }
            ]
        }
        """;

        // Act
        var result = await _rulesEngine.ValidateRuleDefinitionAsync(validRuleJson);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateRuleDefinitionAsync_WithInvalidRule_ShouldReturnFalse()
    {
        // Arrange
        var invalidRuleJson = """
        {
            "name": "",
            "priority": -1,
            "clauses": []
        }
        """;

        // Act
        var result = await _rulesEngine.ValidateRuleDefinitionAsync(invalidRuleJson);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_WithNoMatchingClauses_ShouldReturnDefaultApprove()
    {
        // Arrange
        var application = CreateTestApplication(creditScore: 650, amount: 25000, incomeMonthly: 5000);
        var applicant = CreateTestApplicant();
        var rules = new List<Rule>
        {
            CreateTestRule("No Match Rule", 10, """
            {
                "name": "No Match Rule",
                "priority": 10,
                "clauses": [
                    {
                        "if": "CreditScore > 800",
                        "then": "APPROVE",
                        "reason": "Excellent credit"
                    }
                ]
            }
            """)
        };

        _mockRuleRepository.Setup(r => r.GetActiveRulesAsync())
            .ReturnsAsync(rules);

        // Act
        var result = await _rulesEngine.EvaluateAsync(application, applicant);

        // Assert
        result.Should().NotBeNull();
        result.Outcome.Should().Be(DecisionOutcome.Approve); // Default outcome
        result.Reasons.Should().BeEmpty();
        result.RuleResults.Should().HaveCount(1);
        result.RuleResults[0].Executed.Should().BeFalse();
    }

    private LoanApplication CreateTestApplication(int? creditScore, decimal amount, decimal incomeMonthly)
    {
        return new LoanApplication
        {
            Id = 1,
            AffiliateId = 1,
            ApplicantId = 1,
            ProductType = "Personal Loan",
            Amount = amount,
            IncomeMonthly = incomeMonthly,
            EmploymentType = "Full-Time",
            CreditScore = creditScore,
            Status = ApplicationStatus.Submitted,
            CreatedAt = DateTime.UtcNow
        };
    }

    private Applicant CreateTestApplicant()
    {
        return new Applicant
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            SsnHash = "hashed_ssn",
            DateOfBirth = new DateTime(1990, 1, 1),
            Address = new Address
            {
                Street = "123 Main St",
                City = "Anytown",
                State = "CA",
                ZipCode = "12345"
            },
            Phone = "555-1234",
            Email = "john.doe@example.com"
        };
    }

    private Rule CreateTestRule(string name, int priority, string ruleDefinition)
    {
        return new Rule
        {
            Id = 1,
            Name = name,
            Description = $"Test rule: {name}",
            RuleDefinition = ruleDefinition,
            Priority = priority,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
}