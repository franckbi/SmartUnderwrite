using FluentAssertions;
using SmartUnderwrite.Core.RulesEngine.Models;
using Xunit;

namespace SmartUnderwrite.Tests.RulesEngine.Models;

public class EvaluationContextTests
{
    [Fact]
    public void EvaluationContext_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var context = new EvaluationContext();

        // Assert
        context.Amount.Should().Be(0);
        context.IncomeMonthly.Should().Be(0);
        context.CreditScore.Should().BeNull();
        context.EmploymentType.Should().Be(string.Empty);
        context.ProductType.Should().Be(string.Empty);
        context.ApplicationDate.Should().Be(default(DateTime));
        context.AdditionalProperties.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void EvaluationContext_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var applicationDate = DateTime.UtcNow;
        var additionalProperties = new Dictionary<string, object>
        {
            { "DebtToIncomeRatio", 0.35 },
            { "HasCollateral", true }
        };

        // Act
        var context = new EvaluationContext
        {
            Amount = 25000m,
            IncomeMonthly = 5000m,
            CreditScore = 720,
            EmploymentType = "Full-Time",
            ProductType = "Personal Loan",
            ApplicationDate = applicationDate,
            AdditionalProperties = additionalProperties
        };

        // Assert
        context.Amount.Should().Be(25000m);
        context.IncomeMonthly.Should().Be(5000m);
        context.CreditScore.Should().Be(720);
        context.EmploymentType.Should().Be("Full-Time");
        context.ProductType.Should().Be("Personal Loan");
        context.ApplicationDate.Should().Be(applicationDate);
        context.AdditionalProperties.Should().BeEquivalentTo(additionalProperties);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000.50)]
    [InlineData(100000)]
    [InlineData(-500)] // Edge case
    public void EvaluationContext_WithVariousAmounts_ShouldAcceptAllValues(decimal amount)
    {
        // Act
        var context = new EvaluationContext
        {
            Amount = amount
        };

        // Assert
        context.Amount.Should().Be(amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2500.75)]
    [InlineData(15000)]
    [InlineData(-1000)] // Edge case
    public void EvaluationContext_WithVariousIncomes_ShouldAcceptAllValues(decimal income)
    {
        // Act
        var context = new EvaluationContext
        {
            IncomeMonthly = income
        };

        // Assert
        context.IncomeMonthly.Should().Be(income);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(300)]
    [InlineData(850)]
    public void EvaluationContext_WithVariousCreditScores_ShouldAcceptAllValues(int? creditScore)
    {
        // Act
        var context = new EvaluationContext
        {
            CreditScore = creditScore
        };

        // Assert
        context.CreditScore.Should().Be(creditScore);
    }

    [Theory]
    [InlineData("Full-Time")]
    [InlineData("Part-Time")]
    [InlineData("Contract")]
    [InlineData("Self-Employed")]
    [InlineData("Unemployed")]
    [InlineData("")]
    public void EvaluationContext_WithVariousEmploymentTypes_ShouldAcceptAllValues(string employmentType)
    {
        // Act
        var context = new EvaluationContext
        {
            EmploymentType = employmentType
        };

        // Assert
        context.EmploymentType.Should().Be(employmentType);
    }

    [Theory]
    [InlineData("Personal Loan")]
    [InlineData("Auto Loan")]
    [InlineData("Mortgage")]
    [InlineData("Business Loan")]
    [InlineData("")]
    public void EvaluationContext_WithVariousProductTypes_ShouldAcceptAllValues(string productType)
    {
        // Act
        var context = new EvaluationContext
        {
            ProductType = productType
        };

        // Assert
        context.ProductType.Should().Be(productType);
    }

    [Fact]
    public void EvaluationContext_AddAdditionalProperty_ShouldAddToCollection()
    {
        // Arrange
        var context = new EvaluationContext();

        // Act
        context.AdditionalProperties.Add("TestProperty", "TestValue");

        // Assert
        context.AdditionalProperties.Should().HaveCount(1);
        context.AdditionalProperties.Should().ContainKey("TestProperty");
        context.AdditionalProperties["TestProperty"].Should().Be("TestValue");
    }

    [Fact]
    public void EvaluationContext_WithVariousAdditionalPropertyTypes_ShouldHandleCorrectly()
    {
        // Arrange
        var context = new EvaluationContext();

        // Act
        context.AdditionalProperties.Add("StringProperty", "test");
        context.AdditionalProperties.Add("IntProperty", 42);
        context.AdditionalProperties.Add("DecimalProperty", 123.45m);
        context.AdditionalProperties.Add("BoolProperty", true);
        context.AdditionalProperties.Add("DateProperty", DateTime.UtcNow);
        context.AdditionalProperties.Add("NullProperty", null!);

        // Assert
        context.AdditionalProperties.Should().HaveCount(6);
        context.AdditionalProperties["StringProperty"].Should().Be("test");
        context.AdditionalProperties["IntProperty"].Should().Be(42);
        context.AdditionalProperties["DecimalProperty"].Should().Be(123.45m);
        context.AdditionalProperties["BoolProperty"].Should().Be(true);
        context.AdditionalProperties["DateProperty"].Should().BeOfType<DateTime>();
        context.AdditionalProperties["NullProperty"].Should().BeNull();
    }

    [Fact]
    public void EvaluationContext_WithComplexAdditionalProperties_ShouldHandleCorrectly()
    {
        // Arrange
        var context = new EvaluationContext();
        var complexObject = new { Name = "Test", Value = 100 };
        var list = new List<string> { "item1", "item2", "item3" };

        // Act
        context.AdditionalProperties.Add("ComplexObject", complexObject);
        context.AdditionalProperties.Add("List", list);

        // Assert
        context.AdditionalProperties.Should().HaveCount(2);
        context.AdditionalProperties["ComplexObject"].Should().BeEquivalentTo(complexObject);
        context.AdditionalProperties["List"].Should().BeEquivalentTo(list);
    }

    [Fact]
    public void EvaluationContext_ApplicationDateInPast_ShouldHandleCorrectly()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddDays(-30);

        // Act
        var context = new EvaluationContext
        {
            ApplicationDate = pastDate
        };

        // Assert
        context.ApplicationDate.Should().Be(pastDate);
        context.ApplicationDate.Should().BeBefore(DateTime.UtcNow);
    }

    [Fact]
    public void EvaluationContext_ApplicationDateInFuture_ShouldHandleCorrectly()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(30);

        // Act
        var context = new EvaluationContext
        {
            ApplicationDate = futureDate
        };

        // Assert
        context.ApplicationDate.Should().Be(futureDate);
        context.ApplicationDate.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void EvaluationContext_WithRealWorldScenario_ShouldHandleCorrectly()
    {
        // Arrange & Act
        var context = new EvaluationContext
        {
            Amount = 35000m,
            IncomeMonthly = 6500m,
            CreditScore = 745,
            EmploymentType = "Full-Time",
            ProductType = "Auto Loan",
            ApplicationDate = DateTime.UtcNow.AddDays(-2),
            AdditionalProperties = new Dictionary<string, object>
            {
                { "DebtToIncomeRatio", 0.28 },
                { "HasCollateral", true },
                { "CollateralValue", 40000m },
                { "YearsAtCurrentJob", 3.5 },
                { "HasCosigner", false },
                { "PreviousLoanDefaults", 0 },
                { "BankingRelationshipYears", 5 }
            }
        };

        // Assert
        context.Amount.Should().Be(35000m);
        context.IncomeMonthly.Should().Be(6500m);
        context.CreditScore.Should().Be(745);
        context.EmploymentType.Should().Be("Full-Time");
        context.ProductType.Should().Be("Auto Loan");
        context.ApplicationDate.Should().BeCloseTo(DateTime.UtcNow.AddDays(-2), TimeSpan.FromMinutes(1));
        context.AdditionalProperties.Should().HaveCount(7);
        context.AdditionalProperties["DebtToIncomeRatio"].Should().Be(0.28);
        context.AdditionalProperties["HasCollateral"].Should().Be(true);
        context.AdditionalProperties["CollateralValue"].Should().Be(40000m);
    }

    [Fact]
    public void EvaluationContext_ClearAdditionalProperties_ShouldEmptyCollection()
    {
        // Arrange
        var context = new EvaluationContext();
        context.AdditionalProperties.Add("Test1", "Value1");
        context.AdditionalProperties.Add("Test2", "Value2");

        // Act
        context.AdditionalProperties.Clear();

        // Assert
        context.AdditionalProperties.Should().BeEmpty();
    }

    [Fact]
    public void EvaluationContext_RemoveAdditionalProperty_ShouldRemoveFromCollection()
    {
        // Arrange
        var context = new EvaluationContext();
        context.AdditionalProperties.Add("Test1", "Value1");
        context.AdditionalProperties.Add("Test2", "Value2");

        // Act
        var removed = context.AdditionalProperties.Remove("Test1");

        // Assert
        removed.Should().BeTrue();
        context.AdditionalProperties.Should().HaveCount(1);
        context.AdditionalProperties.Should().ContainKey("Test2");
        context.AdditionalProperties.Should().NotContainKey("Test1");
    }
}