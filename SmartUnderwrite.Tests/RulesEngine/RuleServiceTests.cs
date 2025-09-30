using Microsoft.Extensions.Logging;
using Moq;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Core.RulesEngine.Compilation;
using SmartUnderwrite.Core.RulesEngine.Interfaces;
using SmartUnderwrite.Core.RulesEngine.Models;
using SmartUnderwrite.Core.RulesEngine.Parsing;
using SmartUnderwrite.Core.RulesEngine.Services;
using SmartUnderwrite.Core.RulesEngine.Validation;
using Xunit;
using FluentAssertions;

namespace SmartUnderwrite.Tests.RulesEngine;

public class RuleServiceTests
{
    private readonly Mock<IRuleRepository> _mockRuleRepository;
    private readonly Mock<IRuleVersionRepository> _mockRuleVersionRepository;
    private readonly Mock<ILogger<RuleService>> _mockLogger;
    private readonly IRuleParser _ruleParser;
    private readonly RuleService _ruleService;

    public RuleServiceTests()
    {
        _mockRuleRepository = new Mock<IRuleRepository>();
        _mockRuleVersionRepository = new Mock<IRuleVersionRepository>();
        _mockLogger = new Mock<ILogger<RuleService>>();
        
        var expressionCompiler = new SmartUnderwrite.Core.RulesEngine.Compilation.ExpressionCompiler();
        _ruleParser = new RuleParser(expressionCompiler);
        
        _ruleService = new RuleService(
            _mockRuleRepository.Object,
            _mockRuleVersionRepository.Object,
            _ruleParser,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAllRulesAsync_ShouldReturnAllRules()
    {
        // Arrange
        var rules = new List<Rule>
        {
            CreateTestRule("Rule 1", 10),
            CreateTestRule("Rule 2", 20)
        };

        _mockRuleRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(rules);

        // Act
        var result = await _ruleService.GetAllRulesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(rules);
    }

    [Fact]
    public async Task GetActiveRulesAsync_ShouldReturnActiveRules()
    {
        // Arrange
        var activeRules = new List<Rule>
        {
            CreateTestRule("Active Rule", 10, isActive: true)
        };

        _mockRuleRepository.Setup(r => r.GetActiveRulesAsync())
            .ReturnsAsync(activeRules);

        // Act
        var result = await _ruleService.GetActiveRulesAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetRuleByIdAsync_WithValidId_ShouldReturnRule()
    {
        // Arrange
        var rule = CreateTestRule("Test Rule", 10);
        _mockRuleRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(rule);

        // Act
        var result = await _ruleService.GetRuleByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(rule);
    }

    [Fact]
    public async Task GetRuleByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        _mockRuleRepository.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Rule?)null);

        // Act
        var result = await _ruleService.GetRuleByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateRuleAsync_WithValidRule_ShouldCreateSuccessfully()
    {
        // Arrange
        var ruleDefinition = GetValidRuleDefinition();
        var createdRule = CreateTestRule("New Rule", 10);
        
        _mockRuleRepository.Setup(r => r.CreateAsync(It.IsAny<Rule>()))
            .ReturnsAsync(createdRule);
        
        _mockRuleVersionRepository.Setup(r => r.CreateAsync(It.IsAny<RuleVersion>()))
            .ReturnsAsync(new RuleVersion());

        // Act
        var result = await _ruleService.CreateRuleAsync("New Rule", "Test description", ruleDefinition, 10);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Rule");
        _mockRuleRepository.Verify(r => r.CreateAsync(It.IsAny<Rule>()), Times.Once);
        _mockRuleVersionRepository.Verify(r => r.CreateAsync(It.IsAny<RuleVersion>()), Times.Once);
    }

    [Fact]
    public async Task CreateRuleAsync_WithInvalidRule_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidRuleDefinition = """
        {
            "name": "",
            "priority": -1,
            "clauses": []
        }
        """;

        // Act & Assert
        await _ruleService.Invoking(s => s.CreateRuleAsync("Invalid Rule", "Description", invalidRuleDefinition, 10))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid rule definition*");
    }

    [Fact]
    public async Task UpdateRuleAsync_WithValidRule_ShouldUpdateSuccessfully()
    {
        // Arrange
        var existingRule = CreateTestRule("Existing Rule", 10);
        var ruleDefinition = GetValidRuleDefinition();
        
        _mockRuleRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(existingRule);
        
        _mockRuleRepository.Setup(r => r.UpdateAsync(It.IsAny<Rule>()))
            .ReturnsAsync(existingRule);
        
        _mockRuleVersionRepository.Setup(r => r.CreateAsync(It.IsAny<RuleVersion>()))
            .ReturnsAsync(new RuleVersion());

        // Act
        var result = await _ruleService.UpdateRuleAsync(1, "Updated Rule", "Updated description", ruleDefinition, 15);

        // Assert
        result.Should().NotBeNull();
        _mockRuleRepository.Verify(r => r.UpdateAsync(It.IsAny<Rule>()), Times.Once);
        _mockRuleVersionRepository.Verify(r => r.CreateAsync(It.IsAny<RuleVersion>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRuleAsync_WithNonExistentRule_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockRuleRepository.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Rule?)null);

        // Act & Assert
        await _ruleService.Invoking(s => s.UpdateRuleAsync(999, "Name", "Description", GetValidRuleDefinition(), 10))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task ActivateRuleAsync_WithInactiveRule_ShouldActivateSuccessfully()
    {
        // Arrange
        var inactiveRule = CreateTestRule("Inactive Rule", 10, isActive: false);
        
        _mockRuleRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(inactiveRule);
        
        _mockRuleRepository.Setup(r => r.UpdateAsync(It.IsAny<Rule>()))
            .ReturnsAsync(inactiveRule);
        
        _mockRuleVersionRepository.Setup(r => r.CreateAsync(It.IsAny<RuleVersion>()))
            .ReturnsAsync(new RuleVersion());

        // Act
        var result = await _ruleService.ActivateRuleAsync(1);

        // Assert
        result.Should().NotBeNull();
        _mockRuleRepository.Verify(r => r.UpdateAsync(It.IsAny<Rule>()), Times.Once);
        _mockRuleVersionRepository.Verify(r => r.CreateAsync(It.IsAny<RuleVersion>()), Times.Once);
    }

    [Fact]
    public async Task ActivateRuleAsync_WithActiveRule_ShouldReturnRuleWithoutChanges()
    {
        // Arrange
        var activeRule = CreateTestRule("Active Rule", 10, isActive: true);
        
        _mockRuleRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(activeRule);

        // Act
        var result = await _ruleService.ActivateRuleAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.IsActive.Should().BeTrue();
        _mockRuleRepository.Verify(r => r.UpdateAsync(It.IsAny<Rule>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateRuleAsync_WithActiveRule_ShouldDeactivateSuccessfully()
    {
        // Arrange
        var activeRule = CreateTestRule("Active Rule", 10, isActive: true);
        
        _mockRuleRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(activeRule);
        
        _mockRuleRepository.Setup(r => r.UpdateAsync(It.IsAny<Rule>()))
            .ReturnsAsync(activeRule);
        
        _mockRuleVersionRepository.Setup(r => r.CreateAsync(It.IsAny<RuleVersion>()))
            .ReturnsAsync(new RuleVersion());

        // Act
        var result = await _ruleService.DeactivateRuleAsync(1);

        // Assert
        result.Should().NotBeNull();
        _mockRuleRepository.Verify(r => r.UpdateAsync(It.IsAny<Rule>()), Times.Once);
        _mockRuleVersionRepository.Verify(r => r.CreateAsync(It.IsAny<RuleVersion>()), Times.Once);
    }

    [Fact]
    public async Task DeleteRuleAsync_WithExistingRule_ShouldDeleteSuccessfully()
    {
        // Arrange
        var rule = CreateTestRule("Rule to Delete", 10);
        
        _mockRuleRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(rule);
        
        _mockRuleRepository.Setup(r => r.DeleteAsync(1))
            .ReturnsAsync(true);
        
        _mockRuleVersionRepository.Setup(r => r.CreateAsync(It.IsAny<RuleVersion>()))
            .ReturnsAsync(new RuleVersion());

        // Act
        var result = await _ruleService.DeleteRuleAsync(1);

        // Assert
        result.Should().BeTrue();
        _mockRuleRepository.Verify(r => r.DeleteAsync(1), Times.Once);
        _mockRuleVersionRepository.Verify(r => r.CreateAsync(It.IsAny<RuleVersion>()), Times.Once);
    }

    [Fact]
    public async Task DeleteRuleAsync_WithNonExistentRule_ShouldReturnFalse()
    {
        // Arrange
        _mockRuleRepository.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Rule?)null);

        // Act
        var result = await _ruleService.DeleteRuleAsync(999);

        // Assert
        result.Should().BeFalse();
        _mockRuleRepository.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ValidateRuleDefinitionAsync_WithValidRule_ShouldReturnSuccess()
    {
        // Arrange
        var validRuleDefinition = GetValidRuleDefinition();

        // Act
        var result = await _ruleService.ValidateRuleDefinitionAsync(validRuleDefinition);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateRuleDefinitionAsync_WithInvalidRule_ShouldReturnErrors()
    {
        // Arrange
        var invalidRuleDefinition = """
        {
            "name": "",
            "priority": -1,
            "clauses": []
        }
        """;

        // Act
        var result = await _ruleService.ValidateRuleDefinitionAsync(invalidRuleDefinition);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetRuleHistoryAsync_ShouldReturnVersionHistory()
    {
        // Arrange
        var versions = new List<RuleVersion>
        {
            new RuleVersion { Id = 1, OriginalRuleId = 1, Version = 1 },
            new RuleVersion { Id = 2, OriginalRuleId = 1, Version = 2 }
        };

        _mockRuleVersionRepository.Setup(r => r.GetRuleHistoryAsync(1))
            .ReturnsAsync(versions);

        // Act
        var result = await _ruleService.GetRuleHistoryAsync(1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(versions);
    }

    [Fact]
    public async Task CreateRuleVersionAsync_WithValidRule_ShouldCreateNewVersion()
    {
        // Arrange
        var originalRule = CreateTestRule("Original Rule", 10);
        var ruleDefinition = GetValidRuleDefinition();
        var newRule = CreateTestRule("New Version", 15);
        
        _mockRuleRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(originalRule);
        
        _mockRuleRepository.Setup(r => r.UpdateAsync(It.IsAny<Rule>()))
            .ReturnsAsync(originalRule);
        
        _mockRuleRepository.Setup(r => r.CreateAsync(It.IsAny<Rule>()))
            .ReturnsAsync(newRule);
        
        _mockRuleVersionRepository.Setup(r => r.CreateAsync(It.IsAny<RuleVersion>()))
            .ReturnsAsync(new RuleVersion());

        // Act
        var result = await _ruleService.CreateRuleVersionAsync(1, "New Version", "Updated description", ruleDefinition, 15);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Version");
        _mockRuleRepository.Verify(r => r.UpdateAsync(It.IsAny<Rule>()), Times.Once); // Deactivate original
        _mockRuleRepository.Verify(r => r.CreateAsync(It.IsAny<Rule>()), Times.Once); // Create new version
        _mockRuleVersionRepository.Verify(r => r.CreateAsync(It.IsAny<RuleVersion>()), Times.Exactly(2)); // Two version records
    }

    private Rule CreateTestRule(string name, int priority, bool isActive = true)
    {
        return new Rule
        {
            Id = 1,
            Name = name,
            Description = $"Test rule: {name}",
            RuleDefinition = GetValidRuleDefinition(),
            Priority = priority,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    private string GetValidRuleDefinition()
    {
        return """
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
    }
}