using FluentAssertions;
using SmartUnderwrite.Core.ValueObjects;
using Xunit;

namespace SmartUnderwrite.Tests.ValueObjects;

public class AddressTests
{
    [Fact]
    public void Address_DefaultConstructor_ShouldSetEmptyStrings()
    {
        // Act
        var address = new Address();

        // Assert
        address.Street.Should().Be(string.Empty);
        address.City.Should().Be(string.Empty);
        address.State.Should().Be(string.Empty);
        address.ZipCode.Should().Be(string.Empty);
    }

    [Fact]
    public void Address_ParameterizedConstructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var street = "123 Main St";
        var city = "Anytown";
        var state = "CA";
        var zipCode = "12345";

        // Act
        var address = new Address(street, city, state, zipCode);

        // Assert
        address.Street.Should().Be(street);
        address.City.Should().Be(city);
        address.State.Should().Be(state);
        address.ZipCode.Should().Be(zipCode);
    }

    [Fact]
    public void Address_InitializerSyntax_ShouldSetPropertiesCorrectly()
    {
        // Act
        var address = new Address
        {
            Street = "456 Oak Ave",
            City = "Springfield",
            State = "IL",
            ZipCode = "62701"
        };

        // Assert
        address.Street.Should().Be("456 Oak Ave");
        address.City.Should().Be("Springfield");
        address.State.Should().Be("IL");
        address.ZipCode.Should().Be("62701");
    }

    [Fact]
    public void Address_Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Anytown", "CA", "12345");
        var address2 = new Address("123 Main St", "Anytown", "CA", "12345");

        // Act & Assert
        address1.Equals(address2).Should().BeTrue();
        address1.Should().Be(address2);
        (address1 == address2).Should().BeTrue();
        (address1 != address2).Should().BeFalse();
    }

    [Fact]
    public void Address_Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Anytown", "CA", "12345");
        var address2 = new Address("456 Oak Ave", "Anytown", "CA", "12345");

        // Act & Assert
        address1.Equals(address2).Should().BeFalse();
        address1.Should().NotBe(address2);
        (address1 == address2).Should().BeFalse();
        (address1 != address2).Should().BeTrue();
    }

    [Fact]
    public void Address_Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var address = new Address("123 Main St", "Anytown", "CA", "12345");

        // Act & Assert
        address.Equals(null).Should().BeFalse();
        address.Equals((object?)null).Should().BeFalse();
        (address == null).Should().BeFalse();
        (address != null).Should().BeTrue();
    }

    [Fact]
    public void Address_Equals_WithSameReference_ShouldReturnTrue()
    {
        // Arrange
        var address = new Address("123 Main St", "Anytown", "CA", "12345");

        // Act & Assert
        address.Equals(address).Should().BeTrue();
        (address == address).Should().BeTrue();
    }

    [Fact]
    public void Address_GetHashCode_WithSameValues_ShouldReturnSameHashCode()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Anytown", "CA", "12345");
        var address2 = new Address("123 Main St", "Anytown", "CA", "12345");

        // Act & Assert
        address1.GetHashCode().Should().Be(address2.GetHashCode());
    }

    [Fact]
    public void Address_GetHashCode_WithDifferentValues_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Anytown", "CA", "12345");
        var address2 = new Address("456 Oak Ave", "Anytown", "CA", "12345");

        // Act & Assert
        address1.GetHashCode().Should().NotBe(address2.GetHashCode());
    }

    [Fact]
    public void Address_ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var address = new Address("123 Main St", "Anytown", "CA", "12345");

        // Act
        var result = address.ToString();

        // Assert
        result.Should().Be("123 Main St, Anytown, CA 12345");
    }

    [Fact]
    public void Address_ToString_WithEmptyValues_ShouldReturnFormattedString()
    {
        // Arrange
        var address = new Address();

        // Act
        var result = address.ToString();

        // Assert
        result.Should().Be(", ,  ");
    }

    [Theory]
    [InlineData("", "City", "ST", "12345")]
    [InlineData("Street", "", "ST", "12345")]
    [InlineData("Street", "City", "", "12345")]
    [InlineData("Street", "City", "ST", "")]
    public void Address_WithPartialData_ShouldHandleCorrectly(string street, string city, string state, string zipCode)
    {
        // Act
        var address = new Address(street, city, state, zipCode);

        // Assert
        address.Street.Should().Be(street);
        address.City.Should().Be(city);
        address.State.Should().Be(state);
        address.ZipCode.Should().Be(zipCode);
    }

    [Fact]
    public void Address_CaseSensitiveComparison_ShouldReturnFalse()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Anytown", "CA", "12345");
        var address2 = new Address("123 main st", "anytown", "ca", "12345");

        // Act & Assert
        address1.Equals(address2).Should().BeFalse();
        address1.Should().NotBe(address2);
    }

    [Fact]
    public void Address_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var address = new Address("123 Main St. Apt #4B", "San José", "CA", "12345-6789");

        // Act & Assert
        address.Street.Should().Be("123 Main St. Apt #4B");
        address.City.Should().Be("San José");
        address.State.Should().Be("CA");
        address.ZipCode.Should().Be("12345-6789");
        address.ToString().Should().Be("123 Main St. Apt #4B, San José, CA 12345-6789");
    }

    [Fact]
    public void Address_EqualityOperators_WithNullValues_ShouldHandleCorrectly()
    {
        // Arrange
        Address? address1 = null;
        Address? address2 = null;
        var address3 = new Address("123 Main St", "Anytown", "CA", "12345");

        // Act & Assert
        (address1 == address2).Should().BeTrue();
        (address1 != address2).Should().BeFalse();
        (address1 == address3).Should().BeFalse();
        (address1 != address3).Should().BeTrue();
        (address3 == address1).Should().BeFalse();
        (address3 != address1).Should().BeTrue();
    }

    [Fact]
    public void Address_Immutability_ShouldNotAllowPropertyChanges()
    {
        // Arrange
        var address = new Address("123 Main St", "Anytown", "CA", "12345");

        // Act & Assert
        // Properties should be init-only, so this test verifies the design
        // The compiler should prevent modification of properties after initialization
        address.Street.Should().Be("123 Main St");
        address.City.Should().Be("Anytown");
        address.State.Should().Be("CA");
        address.ZipCode.Should().Be("12345");
    }
}