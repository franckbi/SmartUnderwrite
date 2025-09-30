using SmartUnderwrite.Core.RulesEngine.Compilation;
using SmartUnderwrite.Core.RulesEngine.Models;
using Xunit;
using FluentAssertions;

namespace SmartUnderwrite.Tests.RulesEngine;

public class ExpressionCompilerTests
{
    private readonly ExpressionCompiler _compiler;

    public ExpressionCompilerTests()
    {
        _compiler = new ExpressionCompiler();
    }

    [Fact]
    public void CompileCondition_SimpleComparison_ShouldCompileSuccessfully()
    {
        // Arrange
        var condition = "CreditScore > 600";
        var context = new EvaluationContext { CreditScore = 650 };

        // Act
        var expression = _compiler.CompileCondition(condition);
        var result = expression.Compile()(context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CompileCondition_NullableComparison_ShouldHandleNullValues()
    {
        // Arrange
        var condition = "CreditScore > 600";
        var context = new EvaluationContext { CreditScore = null };

        // Act
        var expression = _compiler.CompileCondition(condition);
        var result = expression.Compile()(context);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CompileCondition_ComplexLogicalExpression_ShouldEvaluateCorrectly()
    {
        // Arrange
        var condition = "CreditScore >= 650 && IncomeMonthly > 5000";
        var context = new EvaluationContext 
        { 
            CreditScore = 700, 
            IncomeMonthly = 6000 
        };

        // Act
        var expression = _compiler.CompileCondition(condition);
        var result = expression.Compile()(context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CompileCondition_OrLogicalExpression_ShouldEvaluateCorrectly()
    {
        // Arrange
        var condition = "CreditScore >= 750 || IncomeMonthly > 10000";
        var context = new EvaluationContext 
        { 
            CreditScore = 600, 
            IncomeMonthly = 12000 
        };

        // Act
        var expression = _compiler.CompileCondition(condition);
        var result = expression.Compile()(context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CompileCondition_WithParentheses_ShouldRespectPrecedence()
    {
        // Arrange
        var condition = "(CreditScore > 600 || IncomeMonthly > 8000) && Amount <= 50000";
        var context = new EvaluationContext 
        { 
            CreditScore = 550, 
            IncomeMonthly = 9000, 
            Amount = 45000 
        };

        // Act
        var expression = _compiler.CompileCondition(condition);
        var result = expression.Compile()(context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CompileCondition_StringComparison_ShouldWork()
    {
        // Arrange
        var condition = "EmploymentType == \"Full-Time\"";
        var context = new EvaluationContext { EmploymentType = "Full-Time" };

        // Act
        var expression = _compiler.CompileCondition(condition);
        var result = expression.Compile()(context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CompileCondition_DecimalComparison_ShouldWork()
    {
        // Arrange
        var condition = "Amount > 25000.50";
        var context = new EvaluationContext { Amount = 30000.75m };

        // Act
        var expression = _compiler.CompileCondition(condition);
        var result = expression.Compile()(context);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CompileCondition_EmptyOrNullCondition_ShouldThrowArgumentException(string condition)
    {
        // Act & Assert
        _compiler.Invoking(c => c.CompileCondition(condition))
            .Should().Throw<ArgumentException>()
            .WithMessage("*cannot be null or empty*");
    }

    [Fact]
    public void CompileCondition_InvalidSyntax_ShouldThrowArgumentException()
    {
        // Arrange
        var condition = "CreditScore > > 600";

        // Act & Assert
        _compiler.Invoking(c => c.CompileCondition(condition))
            .Should().Throw<ArgumentException>()
            .WithMessage("*Failed to compile condition*");
    }

    [Fact]
    public void CompileCondition_UnknownProperty_ShouldThrowArgumentException()
    {
        // Arrange
        var condition = "UnknownProperty > 600";

        // Act & Assert
        _compiler.Invoking(c => c.CompileCondition(condition))
            .Should().Throw<ArgumentException>()
            .WithMessage("*Failed to compile condition*");
    }

    [Theory]
    [InlineData("CreditScore > 600", true)]
    [InlineData("Amount <= 50000", true)]
    [InlineData("EmploymentType == \"Full-Time\"", true)]
    [InlineData("CreditScore >= 650 && IncomeMonthly > 5000", true)]
    [InlineData("InvalidProperty > 600", false)]
    [InlineData("CreditScore > > 600", false)]
    [InlineData("", false)]
    public void ValidateCondition_VariousConditions_ShouldReturnExpectedResult(string condition, bool expected)
    {
        // Act
        var result = _compiler.ValidateCondition(condition);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetAvailableProperties_ShouldReturnExpectedProperties()
    {
        // Act
        var properties = _compiler.GetAvailableProperties().ToList();

        // Assert
        properties.Should().Contain(new[] 
        { 
            "Amount", 
            "IncomeMonthly", 
            "CreditScore", 
            "EmploymentType", 
            "ProductType",
            "ApplicationDate",
            "AdditionalProperties"
        });
    }

    [Fact]
    public void CompileCondition_CaseInsensitivePropertyNames_ShouldWork()
    {
        // Arrange
        var condition = "creditscore > 600"; // lowercase
        var context = new EvaluationContext { CreditScore = 650 };

        // Act
        var expression = _compiler.CompileCondition(condition);
        var result = expression.Compile()(context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CompileCondition_MultipleOperators_ShouldEvaluateCorrectly()
    {
        // Arrange
        var condition = "CreditScore >= 600 && CreditScore <= 800 && IncomeMonthly > 3000";
        var context = new EvaluationContext 
        { 
            CreditScore = 700, 
            IncomeMonthly = 4000 
        };

        // Act
        var expression = _compiler.CompileCondition(condition);
        var result = expression.Compile()(context);

        // Assert
        result.Should().BeTrue();
    }
}