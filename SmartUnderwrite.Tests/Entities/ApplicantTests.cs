using FluentAssertions;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Core.ValueObjects;
using Xunit;

namespace SmartUnderwrite.Tests.Entities;

public class ApplicantTests
{
    [Fact]
    public void Applicant_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var applicant = new Applicant();

        // Assert
        applicant.Id.Should().Be(0);
        applicant.FirstName.Should().Be(string.Empty);
        applicant.LastName.Should().Be(string.Empty);
        applicant.SsnHash.Should().Be(string.Empty);
        applicant.DateOfBirth.Should().Be(default(DateTime));
        applicant.Phone.Should().Be(string.Empty);
        applicant.Email.Should().Be(string.Empty);
        applicant.Address.Should().NotBeNull();
        applicant.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        applicant.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Applicant_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var dateOfBirth = new DateTime(1990, 5, 15);
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var updatedAt = DateTime.UtcNow;
        var address = new Address("123 Main St", "Anytown", "CA", "12345");

        // Act
        var applicant = new Applicant
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            SsnHash = "hashed_ssn_value",
            DateOfBirth = dateOfBirth,
            Phone = "555-123-4567",
            Email = "john.doe@example.com",
            Address = address,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Assert
        applicant.Id.Should().Be(1);
        applicant.FirstName.Should().Be("John");
        applicant.LastName.Should().Be("Doe");
        applicant.SsnHash.Should().Be("hashed_ssn_value");
        applicant.DateOfBirth.Should().Be(dateOfBirth);
        applicant.Phone.Should().Be("555-123-4567");
        applicant.Email.Should().Be("john.doe@example.com");
        applicant.Address.Should().Be(address);
        applicant.CreatedAt.Should().Be(createdAt);
        applicant.UpdatedAt.Should().Be(updatedAt);
    }

    [Theory]
    [InlineData("John", "Doe")]
    [InlineData("Mary", "Smith")]
    [InlineData("José", "García")]
    [InlineData("李", "王")]
    [InlineData("", "")] // Edge case - empty names
    public void Applicant_WithVariousNames_ShouldAcceptAllValues(string firstName, string lastName)
    {
        // Act
        var applicant = new Applicant
        {
            FirstName = firstName,
            LastName = lastName
        };

        // Assert
        applicant.FirstName.Should().Be(firstName);
        applicant.LastName.Should().Be(lastName);
    }

    [Theory]
    [InlineData("john.doe@example.com")]
    [InlineData("mary.smith+test@company.co.uk")]
    [InlineData("user123@domain.org")]
    [InlineData("")]
    [InlineData("invalid-email")] // Edge case - invalid format
    public void Applicant_WithVariousEmails_ShouldAcceptAllValues(string email)
    {
        // Act
        var applicant = new Applicant
        {
            Email = email
        };

        // Assert
        applicant.Email.Should().Be(email);
    }

    [Theory]
    [InlineData("555-123-4567")]
    [InlineData("(555) 123-4567")]
    [InlineData("5551234567")]
    [InlineData("+1-555-123-4567")]
    [InlineData("")]
    public void Applicant_WithVariousPhoneNumbers_ShouldAcceptAllValues(string phone)
    {
        // Act
        var applicant = new Applicant
        {
            Phone = phone
        };

        // Assert
        applicant.Phone.Should().Be(phone);
    }

    [Fact]
    public void Applicant_WithDateOfBirthInPast_ShouldAcceptValue()
    {
        // Arrange
        var dateOfBirth = new DateTime(1985, 3, 20);

        // Act
        var applicant = new Applicant
        {
            DateOfBirth = dateOfBirth
        };

        // Assert
        applicant.DateOfBirth.Should().Be(dateOfBirth);
        applicant.DateOfBirth.Should().BeBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Applicant_WithDateOfBirthInFuture_ShouldAcceptValue()
    {
        // Arrange - Edge case
        var dateOfBirth = DateTime.UtcNow.AddYears(1);

        // Act
        var applicant = new Applicant
        {
            DateOfBirth = dateOfBirth
        };

        // Assert
        applicant.DateOfBirth.Should().Be(dateOfBirth);
        applicant.DateOfBirth.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void Applicant_CalculateAge_ShouldReturnCorrectAge()
    {
        // Arrange
        var dateOfBirth = DateTime.UtcNow.AddYears(-30).AddDays(-100);
        var applicant = new Applicant
        {
            DateOfBirth = dateOfBirth
        };

        // Act
        var age = DateTime.UtcNow.Year - applicant.DateOfBirth.Year;
        if (DateTime.UtcNow.DayOfYear < applicant.DateOfBirth.DayOfYear)
            age--;

        // Assert
        age.Should().Be(30);
    }

    [Fact]
    public void Applicant_WithComplexAddress_ShouldHandleCorrectly()
    {
        // Arrange
        var address = new Address(
            "1234 Main Street, Apt 5B",
            "San Francisco",
            "CA",
            "94102-1234"
        );

        // Act
        var applicant = new Applicant
        {
            Address = address
        };

        // Assert
        applicant.Address.Should().Be(address);
        applicant.Address.Street.Should().Be("1234 Main Street, Apt 5B");
        applicant.Address.City.Should().Be("San Francisco");
        applicant.Address.State.Should().Be("CA");
        applicant.Address.ZipCode.Should().Be("94102-1234");
    }

    [Fact]
    public void Applicant_WithSsnHash_ShouldNotStoreActualSsn()
    {
        // Act
        var applicant = new Applicant
        {
            SsnHash = "sha256_hashed_value_of_ssn"
        };

        // Assert
        applicant.SsnHash.Should().Be("sha256_hashed_value_of_ssn");
        applicant.SsnHash.Should().NotContain("123-45-6789"); // Should not contain actual SSN
        applicant.SsnHash.Should().NotMatch(@"\d{3}-\d{2}-\d{4}"); // Should not match SSN pattern
    }

    [Fact]
    public void Applicant_WithSpecialCharactersInName_ShouldHandleCorrectly()
    {
        // Act
        var applicant = new Applicant
        {
            FirstName = "Jean-Luc",
            LastName = "O'Connor-Smith"
        };

        // Assert
        applicant.FirstName.Should().Be("Jean-Luc");
        applicant.LastName.Should().Be("O'Connor-Smith");
    }

    [Fact]
    public void Applicant_WithLongNames_ShouldHandleCorrectly()
    {
        // Arrange
        var longFirstName = "Wolfeschlegelsteinhausenbergerdorff";
        var longLastName = "Hubert Blaine Wolfeschlegelsteinhausenbergerdorff Sr.";

        // Act
        var applicant = new Applicant
        {
            FirstName = longFirstName,
            LastName = longLastName
        };

        // Assert
        applicant.FirstName.Should().Be(longFirstName);
        applicant.LastName.Should().Be(longLastName);
    }

    [Fact]
    public void Applicant_UpdatedAtNull_ShouldIndicateNoUpdates()
    {
        // Act
        var applicant = new Applicant
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Assert
        applicant.UpdatedAt.Should().BeNull();
        applicant.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Applicant_UpdatedAtSet_ShouldIndicateUpdate()
    {
        // Arrange
        var updatedAt = DateTime.UtcNow;

        // Act
        var applicant = new Applicant
        {
            FirstName = "John",
            LastName = "Doe",
            UpdatedAt = updatedAt
        };

        // Assert
        applicant.UpdatedAt.Should().Be(updatedAt);
        applicant.UpdatedAt.Should().BeAfter(applicant.CreatedAt.AddSeconds(-1));
    }

    [Fact]
    public void Applicant_WithRealWorldData_ShouldHandleCorrectly()
    {
        // Arrange & Act
        var applicant = new Applicant
        {
            Id = 12345,
            FirstName = "Maria",
            LastName = "Rodriguez-Garcia",
            SsnHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            DateOfBirth = new DateTime(1988, 11, 23),
            Phone = "(555) 987-6543",
            Email = "maria.rodriguez@email.com",
            Address = new Address(
                "789 Elm Street, Unit 12",
                "Los Angeles",
                "CA",
                "90210-1234"
            ),
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddHours(-2)
        };

        // Assert
        applicant.Id.Should().Be(12345);
        applicant.FirstName.Should().Be("Maria");
        applicant.LastName.Should().Be("Rodriguez-Garcia");
        applicant.SsnHash.Should().Be("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
        applicant.DateOfBirth.Should().Be(new DateTime(1988, 11, 23));
        applicant.Phone.Should().Be("(555) 987-6543");
        applicant.Email.Should().Be("maria.rodriguez@email.com");
        applicant.Address.Street.Should().Be("789 Elm Street, Unit 12");
        applicant.Address.City.Should().Be("Los Angeles");
        applicant.Address.State.Should().Be("CA");
        applicant.Address.ZipCode.Should().Be("90210-1234");
        applicant.CreatedAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(-5), TimeSpan.FromMinutes(1));
        applicant.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(-2), TimeSpan.FromMinutes(1));
    }
}