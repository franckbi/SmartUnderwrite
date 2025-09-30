using FluentAssertions;
using SmartUnderwrite.Core.Entities;
using Xunit;

namespace SmartUnderwrite.Tests.Entities;

public class DocumentTests
{
    [Fact]
    public void Document_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var document = new Document();

        // Assert
        document.Id.Should().Be(0);
        document.LoanApplicationId.Should().Be(0);
        document.FileName.Should().Be(string.Empty);
        document.ContentType.Should().Be(string.Empty);
        document.StoragePath.Should().Be(string.Empty);
        document.FileSize.Should().Be(0);
        document.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        document.LoanApplication.Should().BeNull();
    }

    [Fact]
    public void Document_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddMinutes(-30);

        // Act
        var document = new Document
        {
            Id = 1,
            LoanApplicationId = 100,
            FileName = "paystub.pdf",
            ContentType = "application/pdf",
            StoragePath = "/documents/2024/01/paystub_12345.pdf",
            FileSize = 1024000,
            CreatedAt = createdAt
        };

        // Assert
        document.Id.Should().Be(1);
        document.LoanApplicationId.Should().Be(100);
        document.FileName.Should().Be("paystub.pdf");
        document.ContentType.Should().Be("application/pdf");
        document.StoragePath.Should().Be("/documents/2024/01/paystub_12345.pdf");
        document.FileSize.Should().Be(1024000);
        document.CreatedAt.Should().Be(createdAt);
    }

    [Theory]
    [InlineData("document.pdf")]
    [InlineData("paystub_january_2024.pdf")]
    [InlineData("bank-statement.jpg")]
    [InlineData("tax_return_2023.docx")]
    [InlineData("employment_verification.png")]
    [InlineData("")]
    public void Document_WithVariousFileNames_ShouldAcceptAllValues(string fileName)
    {
        // Act
        var document = new Document
        {
            FileName = fileName
        };

        // Assert
        document.FileName.Should().Be(fileName);
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("text/plain")]
    [InlineData("")]
    public void Document_WithVariousContentTypes_ShouldAcceptAllValues(string contentType)
    {
        // Act
        var document = new Document
        {
            ContentType = contentType
        };

        // Assert
        document.ContentType.Should().Be(contentType);
    }

    [Theory]
    [InlineData("/documents/2024/01/file.pdf")]
    [InlineData("s3://bucket/documents/file.pdf")]
    [InlineData("https://storage.example.com/documents/file.pdf")]
    [InlineData("C:\\Documents\\file.pdf")]
    [InlineData("")]
    public void Document_WithVariousStoragePaths_ShouldAcceptAllValues(string storagePath)
    {
        // Act
        var document = new Document
        {
            StoragePath = storagePath
        };

        // Assert
        document.StoragePath.Should().Be(storagePath);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1024)]
    [InlineData(1048576)] // 1 MB
    [InlineData(10485760)] // 10 MB
    [InlineData(104857600)] // 100 MB
    public void Document_WithVariousFileSizes_ShouldAcceptAllValues(long fileSize)
    {
        // Act
        var document = new Document
        {
            FileSize = fileSize
        };

        // Assert
        document.FileSize.Should().Be(fileSize);
    }

    [Fact]
    public void Document_WithNegativeFileSize_ShouldAcceptValue()
    {
        // Act - Edge case
        var document = new Document
        {
            FileSize = -1
        };

        // Assert
        document.FileSize.Should().Be(-1);
    }

    [Fact]
    public void Document_NavigationProperty_ShouldInitializeCorrectly()
    {
        // Act
        var document = new Document();

        // Assert
        document.LoanApplication.Should().BeNull();
    }

    [Fact]
    public void Document_WithLoanApplicationReference_ShouldSetCorrectly()
    {
        // Arrange
        var loanApplication = new LoanApplication
        {
            Id = 100,
            Amount = 25000m,
            ProductType = "Personal Loan"
        };

        // Act
        var document = new Document
        {
            LoanApplicationId = 100,
            LoanApplication = loanApplication
        };

        // Assert
        document.LoanApplicationId.Should().Be(100);
        document.LoanApplication.Should().Be(loanApplication);
        document.LoanApplication.Id.Should().Be(100);
    }

    [Fact]
    public void Document_WithSpecialCharactersInFileName_ShouldHandleCorrectly()
    {
        // Act
        var document = new Document
        {
            FileName = "tax_return_2023 (final).pdf",
            StoragePath = "/documents/2024/tax_return_2023_(final)_abc123.pdf"
        };

        // Assert
        document.FileName.Should().Be("tax_return_2023 (final).pdf");
        document.StoragePath.Should().Be("/documents/2024/tax_return_2023_(final)_abc123.pdf");
    }

    [Fact]
    public void Document_WithUnicodeCharactersInFileName_ShouldHandleCorrectly()
    {
        // Act
        var document = new Document
        {
            FileName = "déclaration_fiscale_2023.pdf",
            StoragePath = "/documents/2024/déclaration_fiscale_2023_abc123.pdf"
        };

        // Assert
        document.FileName.Should().Be("déclaration_fiscale_2023.pdf");
        document.StoragePath.Should().Be("/documents/2024/déclaration_fiscale_2023_abc123.pdf");
    }

    [Fact]
    public void Document_WithLongFileName_ShouldHandleCorrectly()
    {
        // Arrange
        var longFileName = "very_long_document_name_that_exceeds_normal_expectations_for_file_naming_conventions_2024.pdf";

        // Act
        var document = new Document
        {
            FileName = longFileName
        };

        // Assert
        document.FileName.Should().Be(longFileName);
    }

    [Fact]
    public void Document_WithLargeFileSize_ShouldHandleCorrectly()
    {
        // Arrange
        var largeFileSize = 1073741824L; // 1 GB

        // Act
        var document = new Document
        {
            FileSize = largeFileSize
        };

        // Assert
        document.FileSize.Should().Be(largeFileSize);
    }

    [Fact]
    public void Document_WithRealWorldData_ShouldHandleCorrectly()
    {
        // Arrange & Act
        var document = new Document
        {
            Id = 12345,
            LoanApplicationId = 67890,
            FileName = "bank_statement_december_2023.pdf",
            ContentType = "application/pdf",
            StoragePath = "s3://smartunderwrite-documents/2024/01/15/bank_statement_december_2023_67890_12345.pdf",
            FileSize = 2048576, // ~2 MB
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };

        // Assert
        document.Id.Should().Be(12345);
        document.LoanApplicationId.Should().Be(67890);
        document.FileName.Should().Be("bank_statement_december_2023.pdf");
        document.ContentType.Should().Be("application/pdf");
        document.StoragePath.Should().Be("s3://smartunderwrite-documents/2024/01/15/bank_statement_december_2023_67890_12345.pdf");
        document.FileSize.Should().Be(2048576);
        document.CreatedAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(-2), TimeSpan.FromMinutes(1));
    }

    [Theory]
    [InlineData("application/pdf", ".pdf")]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx")]
    [InlineData("text/plain", ".txt")]
    public void Document_ContentTypeAndFileExtension_ShouldBeConsistent(string contentType, string expectedExtension)
    {
        // Act
        var document = new Document
        {
            FileName = $"document{expectedExtension}",
            ContentType = contentType
        };

        // Assert
        document.ContentType.Should().Be(contentType);
        document.FileName.Should().EndWith(expectedExtension);
    }

    [Fact]
    public void Document_FileSizeInBytes_ShouldCalculateCorrectly()
    {
        // Arrange
        var fileSizeInKB = 1024L;
        var fileSizeInMB = 1024L * 1024L;
        var fileSizeInGB = 1024L * 1024L * 1024L;

        // Act
        var documentKB = new Document { FileSize = fileSizeInKB };
        var documentMB = new Document { FileSize = fileSizeInMB };
        var documentGB = new Document { FileSize = fileSizeInGB };

        // Assert
        documentKB.FileSize.Should().Be(1024L);
        documentMB.FileSize.Should().Be(1048576L);
        documentGB.FileSize.Should().Be(1073741824L);
    }

    [Fact]
    public void Document_CreatedAtInPast_ShouldBeValid()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddDays(-7);

        // Act
        var document = new Document
        {
            CreatedAt = pastDate
        };

        // Assert
        document.CreatedAt.Should().Be(pastDate);
        document.CreatedAt.Should().BeBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Document_CreatedAtInFuture_ShouldBeValid()
    {
        // Arrange - Edge case
        var futureDate = DateTime.UtcNow.AddMinutes(5);

        // Act
        var document = new Document
        {
            CreatedAt = futureDate
        };

        // Assert
        document.CreatedAt.Should().Be(futureDate);
        document.CreatedAt.Should().BeAfter(DateTime.UtcNow);
    }
}