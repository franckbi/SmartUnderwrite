using FluentAssertions;
using SmartUnderwrite.Core.Entities;
using Xunit;

namespace SmartUnderwrite.Tests.Entities;

public class AffiliateTests
{
    [Fact]
    public void Affiliate_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var affiliate = new Affiliate();

        // Assert
        affiliate.Id.Should().Be(0);
        affiliate.Name.Should().Be(string.Empty);
        affiliate.ExternalId.Should().Be(string.Empty);
        affiliate.IsActive.Should().BeTrue();
        affiliate.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        affiliate.UpdatedAt.Should().BeNull();
        affiliate.Users.Should().NotBeNull().And.BeEmpty();
        affiliate.LoanApplications.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Affiliate_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddDays(-30);
        var updatedAt = DateTime.UtcNow.AddDays(-1);

        // Act
        var affiliate = new Affiliate
        {
            Id = 1,
            Name = "ABC Financial Services",
            ExternalId = "EXT-12345",
            IsActive = true,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Assert
        affiliate.Id.Should().Be(1);
        affiliate.Name.Should().Be("ABC Financial Services");
        affiliate.ExternalId.Should().Be("EXT-12345");
        affiliate.IsActive.Should().BeTrue();
        affiliate.CreatedAt.Should().Be(createdAt);
        affiliate.UpdatedAt.Should().Be(updatedAt);
    }

    [Theory]
    [InlineData("ABC Financial Services")]
    [InlineData("XYZ Lending Corp")]
    [InlineData("Quick Loans LLC")]
    [InlineData("First National Bank")]
    [InlineData("")]
    public void Affiliate_WithVariousNames_ShouldAcceptAllValues(string name)
    {
        // Act
        var affiliate = new Affiliate
        {
            Name = name
        };

        // Assert
        affiliate.Name.Should().Be(name);
    }

    [Theory]
    [InlineData("EXT-12345")]
    [InlineData("PARTNER-ABC")]
    [InlineData("ID_999")]
    [InlineData("")]
    [InlineData("VERY-LONG-EXTERNAL-ID-WITH-SPECIAL-CHARS-123")]
    public void Affiliate_WithVariousExternalIds_ShouldAcceptAllValues(string externalId)
    {
        // Act
        var affiliate = new Affiliate
        {
            ExternalId = externalId
        };

        // Assert
        affiliate.ExternalId.Should().Be(externalId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Affiliate_WithDifferentActiveStates_ShouldSetCorrectly(bool isActive)
    {
        // Act
        var affiliate = new Affiliate
        {
            IsActive = isActive
        };

        // Assert
        affiliate.IsActive.Should().Be(isActive);
    }

    [Fact]
    public void Affiliate_AddUser_ShouldAddToCollection()
    {
        // Arrange
        var affiliate = new Affiliate();
        var user = new User
        {
            Id = 1,
            UserName = "testuser",
            Email = "test@example.com"
        };

        // Act
        affiliate.Users.Add(user);

        // Assert
        affiliate.Users.Should().HaveCount(1);
        affiliate.Users.Should().Contain(user);
    }

    [Fact]
    public void Affiliate_AddLoanApplication_ShouldAddToCollection()
    {
        // Arrange
        var affiliate = new Affiliate();
        var application = new LoanApplication
        {
            Id = 1,
            Amount = 25000m,
            ProductType = "Personal Loan"
        };

        // Act
        affiliate.LoanApplications.Add(application);

        // Assert
        affiliate.LoanApplications.Should().HaveCount(1);
        affiliate.LoanApplications.Should().Contain(application);
    }

    [Fact]
    public void Affiliate_WithMultipleUsers_ShouldHandleCorrectly()
    {
        // Arrange
        var affiliate = new Affiliate();
        var users = new List<User>
        {
            new() { Id = 1, UserName = "user1", Email = "user1@example.com" },
            new() { Id = 2, UserName = "user2", Email = "user2@example.com" },
            new() { Id = 3, UserName = "user3", Email = "user3@example.com" }
        };

        // Act
        foreach (var user in users)
        {
            affiliate.Users.Add(user);
        }

        // Assert
        affiliate.Users.Should().HaveCount(3);
        affiliate.Users.Should().Contain(users[0]);
        affiliate.Users.Should().Contain(users[1]);
        affiliate.Users.Should().Contain(users[2]);
    }

    [Fact]
    public void Affiliate_WithMultipleLoanApplications_ShouldHandleCorrectly()
    {
        // Arrange
        var affiliate = new Affiliate();
        var applications = new List<LoanApplication>
        {
            new() { Id = 1, Amount = 10000m, ProductType = "Personal Loan" },
            new() { Id = 2, Amount = 25000m, ProductType = "Auto Loan" },
            new() { Id = 3, Amount = 50000m, ProductType = "Business Loan" }
        };

        // Act
        foreach (var application in applications)
        {
            affiliate.LoanApplications.Add(application);
        }

        // Assert
        affiliate.LoanApplications.Should().HaveCount(3);
        affiliate.LoanApplications.Should().Contain(applications[0]);
        affiliate.LoanApplications.Should().Contain(applications[1]);
        affiliate.LoanApplications.Should().Contain(applications[2]);
    }

    [Fact]
    public void Affiliate_NavigationProperties_ShouldInitializeCorrectly()
    {
        // Act
        var affiliate = new Affiliate();

        // Assert
        affiliate.Users.Should().NotBeNull().And.BeEmpty();
        affiliate.LoanApplications.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Affiliate_DeactivatedAffiliate_ShouldSetIsActiveFalse()
    {
        // Act
        var affiliate = new Affiliate
        {
            Name = "Deactivated Partner",
            IsActive = false,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        affiliate.IsActive.Should().BeFalse();
        affiliate.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Affiliate_WithSpecialCharactersInName_ShouldHandleCorrectly()
    {
        // Act
        var affiliate = new Affiliate
        {
            Name = "ABC Financial & Investment Services, LLC",
            ExternalId = "ABC-FIN&INV-2024"
        };

        // Assert
        affiliate.Name.Should().Be("ABC Financial & Investment Services, LLC");
        affiliate.ExternalId.Should().Be("ABC-FIN&INV-2024");
    }

    [Fact]
    public void Affiliate_WithLongName_ShouldHandleCorrectly()
    {
        // Arrange
        var longName = "Very Long Financial Services Company Name That Exceeds Normal Length Expectations LLC";

        // Act
        var affiliate = new Affiliate
        {
            Name = longName
        };

        // Assert
        affiliate.Name.Should().Be(longName);
    }

    [Fact]
    public void Affiliate_UpdatedAtNull_ShouldIndicateNoUpdates()
    {
        // Act
        var affiliate = new Affiliate
        {
            Name = "Test Affiliate"
        };

        // Assert
        affiliate.UpdatedAt.Should().BeNull();
        affiliate.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Affiliate_UpdatedAtSet_ShouldIndicateUpdate()
    {
        // Arrange
        var updatedAt = DateTime.UtcNow;

        // Act
        var affiliate = new Affiliate
        {
            Name = "Updated Affiliate",
            UpdatedAt = updatedAt
        };

        // Assert
        affiliate.UpdatedAt.Should().Be(updatedAt);
        affiliate.UpdatedAt.Should().BeAfter(affiliate.CreatedAt.AddSeconds(-1));
    }

    [Fact]
    public void Affiliate_WithRealWorldData_ShouldHandleCorrectly()
    {
        // Arrange & Act
        var affiliate = new Affiliate
        {
            Id = 100,
            Name = "Premier Financial Solutions",
            ExternalId = "PFS-2024-001",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-6),
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
        };

        // Add some users
        affiliate.Users.Add(new User { Id = 1, UserName = "manager1", Email = "manager@pfs.com" });
        affiliate.Users.Add(new User { Id = 2, UserName = "agent1", Email = "agent1@pfs.com" });

        // Add some loan applications
        affiliate.LoanApplications.Add(new LoanApplication { Id = 1, Amount = 15000m, ProductType = "Personal Loan" });
        affiliate.LoanApplications.Add(new LoanApplication { Id = 2, Amount = 35000m, ProductType = "Auto Loan" });

        // Assert
        affiliate.Id.Should().Be(100);
        affiliate.Name.Should().Be("Premier Financial Solutions");
        affiliate.ExternalId.Should().Be("PFS-2024-001");
        affiliate.IsActive.Should().BeTrue();
        affiliate.CreatedAt.Should().BeCloseTo(DateTime.UtcNow.AddMonths(-6), TimeSpan.FromDays(1));
        affiliate.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(-10), TimeSpan.FromHours(1));
        affiliate.Users.Should().HaveCount(2);
        affiliate.LoanApplications.Should().HaveCount(2);
    }

    [Fact]
    public void Affiliate_ClearUsers_ShouldEmptyCollection()
    {
        // Arrange
        var affiliate = new Affiliate();
        affiliate.Users.Add(new User { Id = 1, UserName = "user1" });
        affiliate.Users.Add(new User { Id = 2, UserName = "user2" });

        // Act
        affiliate.Users.Clear();

        // Assert
        affiliate.Users.Should().BeEmpty();
    }

    [Fact]
    public void Affiliate_ClearLoanApplications_ShouldEmptyCollection()
    {
        // Arrange
        var affiliate = new Affiliate();
        affiliate.LoanApplications.Add(new LoanApplication { Id = 1, Amount = 10000m });
        affiliate.LoanApplications.Add(new LoanApplication { Id = 2, Amount = 20000m });

        // Act
        affiliate.LoanApplications.Clear();

        // Assert
        affiliate.LoanApplications.Should().BeEmpty();
    }
}