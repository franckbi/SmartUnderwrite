using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartUnderwrite.Api.Models.Document;
using SmartUnderwrite.Api.Services;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Core.Enums;
using SmartUnderwrite.Core.ValueObjects;
using SmartUnderwrite.Infrastructure.Data;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace SmartUnderwrite.Tests.Services;

public class DocumentServiceTests : IDisposable
{
    private readonly SmartUnderwriteDbContext _context;
    private readonly Mock<IStorageService> _mockStorageService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<ILogger<DocumentService>> _mockLogger;
    private readonly DocumentService _service;

    public DocumentServiceTests()
    {
        var options = new DbContextOptionsBuilder<SmartUnderwriteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SmartUnderwriteDbContext(options);
        _mockStorageService = new Mock<IStorageService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockLogger = new Mock<ILogger<DocumentService>>();
        
        _service = new DocumentService(_context, _mockStorageService.Object, _mockCurrentUserService.Object, _mockLogger.Object);

        SeedTestData();
    }

    [Fact]
    public async Task UploadDocumentAsync_ValidRequest_UploadsDocument()
    {
        // Arrange
        var adminUser = CreateClaimsPrincipal("admin@test.com", new[] { "Admin" });
        var mockFile = CreateMockFile("test.pdf", "application/pdf", 1024);
        var request = new DocumentUploadRequest
        {
            LoanApplicationId = 1,
            File = mockFile.Object,
            Description = "Test document"
        };

        _mockStorageService
            .Setup(x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("applications/1/2024/01/01/abc123_test.pdf");

        // Act
        var result = await _service.UploadDocumentAsync(request, adminUser);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test.pdf", result.FileName);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(1024, result.FileSize);

        // Verify document was saved to database
        var documentInDb = await _context.Documents.FirstOrDefaultAsync(d => d.Id == result.Id);
        Assert.NotNull(documentInDb);
        Assert.Equal(1, documentInDb.LoanApplicationId);
        Assert.Equal("applications/1/2024/01/01/abc123_test.pdf", documentInDb.StoragePath);

        // Verify storage service was called
        _mockStorageService.Verify(x => x.UploadFileAsync(
            It.IsAny<Stream>(),
            "test.pdf",
            "application/pdf",
            "applications/1"), Times.Once);
    }

    [Fact]
    public async Task UploadDocumentAsync_AffiliateUser_CanUploadToOwnApplication()
    {
        // Arrange
        var affiliateUser = CreateClaimsPrincipal("affiliate1@test.com", new[] { "Affiliate" });
        _mockCurrentUserService.Setup(x => x.GetAffiliateId()).Returns(1);
        
        var mockFile = CreateMockFile("test.pdf", "application/pdf", 1024);
        var request = new DocumentUploadRequest
        {
            LoanApplicationId = 1,
            File = mockFile.Object
        };

        _mockStorageService
            .Setup(x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("applications/1/2024/01/01/abc123_test.pdf");

        // Act
        var result = await _service.UploadDocumentAsync(request, affiliateUser);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test.pdf", result.FileName);
    }

    [Fact]
    public async Task UploadDocumentAsync_AffiliateUser_CannotUploadToOtherApplication()
    {
        // Arrange
        var affiliateUser = CreateClaimsPrincipal("affiliate1@test.com", new[] { "Affiliate" });
        _mockCurrentUserService.Setup(x => x.GetAffiliateId()).Returns(1);
        
        var mockFile = CreateMockFile("test.pdf", "application/pdf", 1024);
        var request = new DocumentUploadRequest
        {
            LoanApplicationId = 2, // Application belongs to affiliate 2
            File = mockFile.Object
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _service.UploadDocumentAsync(request, affiliateUser));
    }

    [Fact]
    public async Task UploadDocumentAsync_InvalidFileType_ThrowsArgumentException()
    {
        // Arrange
        var adminUser = CreateClaimsPrincipal("admin@test.com", new[] { "Admin" });
        var mockFile = CreateMockFile("test.exe", "application/x-executable", 1024);
        var request = new DocumentUploadRequest
        {
            LoanApplicationId = 1,
            File = mockFile.Object
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.UploadDocumentAsync(request, adminUser));
        
        Assert.Contains("File type", exception.Message);
    }

    [Fact]
    public async Task UploadDocumentAsync_FileTooLarge_ThrowsArgumentException()
    {
        // Arrange
        var adminUser = CreateClaimsPrincipal("admin@test.com", new[] { "Admin" });
        var mockFile = CreateMockFile("test.pdf", "application/pdf", 11 * 1024 * 1024); // 11MB
        var request = new DocumentUploadRequest
        {
            LoanApplicationId = 1,
            File = mockFile.Object
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.UploadDocumentAsync(request, adminUser));
        
        Assert.Contains("File size cannot exceed", exception.Message);
    }

    [Fact]
    public async Task GetDocumentAsync_ValidRequest_ReturnsDocument()
    {
        // Arrange
        var adminUser = CreateClaimsPrincipal("admin@test.com", new[] { "Admin" });
        var mockStream = new MemoryStream(Encoding.UTF8.GetBytes("test content"));
        
        _mockStorageService
            .Setup(x => x.GetFileAsync("storage/path/test.pdf"))
            .ReturnsAsync(mockStream);

        // Act
        var result = await _service.GetDocumentAsync(1, adminUser);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test.pdf", result.FileName);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(1024, result.FileSize);
        Assert.NotNull(result.FileStream);

        _mockStorageService.Verify(x => x.GetFileAsync("storage/path/test.pdf"), Times.Once);
    }

    [Fact]
    public async Task GetDocumentAsync_AffiliateUser_CannotAccessOtherAffiliateDocument()
    {
        // Arrange
        var affiliateUser = CreateClaimsPrincipal("affiliate1@test.com", new[] { "Affiliate" });
        _mockCurrentUserService.Setup(x => x.GetAffiliateId()).Returns(1);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _service.GetDocumentAsync(2, affiliateUser)); // Document belongs to affiliate 2
    }

    [Fact]
    public async Task DeleteDocumentAsync_ValidRequest_DeletesDocument()
    {
        // Arrange
        var adminUser = CreateClaimsPrincipal("admin@test.com", new[] { "Admin" });
        
        _mockStorageService
            .Setup(x => x.DeleteFileAsync("storage/path/test.pdf"))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteDocumentAsync(1, adminUser);

        // Assert
        Assert.True(result);

        // Verify document was removed from database
        var documentInDb = await _context.Documents.FindAsync(1);
        Assert.Null(documentInDb);

        _mockStorageService.Verify(x => x.DeleteFileAsync("storage/path/test.pdf"), Times.Once);
    }

    [Fact]
    public async Task GetApplicationDocumentsAsync_ValidRequest_ReturnsDocuments()
    {
        // Arrange
        var adminUser = CreateClaimsPrincipal("admin@test.com", new[] { "Admin" });

        // Act
        var result = await _service.GetApplicationDocumentsAsync(1, adminUser);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("test.pdf", result[0].FileName);
    }

    [Fact]
    public async Task GetApplicationDocumentsAsync_AffiliateUser_OnlyReturnsOwnDocuments()
    {
        // Arrange
        var affiliateUser = CreateClaimsPrincipal("affiliate1@test.com", new[] { "Affiliate" });
        _mockCurrentUserService.Setup(x => x.GetAffiliateId()).Returns(1);

        // Act
        var result = await _service.GetApplicationDocumentsAsync(1, affiliateUser);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }

    private Mock<IFormFile> CreateMockFile(string fileName, string contentType, long length)
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.ContentType).Returns(contentType);
        mockFile.Setup(f => f.Length).Returns(length);
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[length]));
        return mockFile;
    }

    private void SeedTestData()
    {
        // Create affiliates
        var affiliate1 = new Affiliate
        {
            Id = 1,
            Name = "Test Affiliate 1",
            ExternalId = "TEST001",
            IsActive = true
        };

        var affiliate2 = new Affiliate
        {
            Id = 2,
            Name = "Test Affiliate 2",
            ExternalId = "TEST002",
            IsActive = true
        };

        _context.Affiliates.AddRange(affiliate1, affiliate2);

        // Create applicants
        var applicant1 = new Applicant
        {
            Id = 1,
            FirstName = "Test",
            LastName = "User1",
            SsnHash = "hash1",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "555-0001",
            Email = "test1@example.com",
            Address = new Address
            {
                Street = "123 Test St",
                City = "Test City",
                State = "TS",
                ZipCode = "12345"
            }
        };

        var applicant2 = new Applicant
        {
            Id = 2,
            FirstName = "Test",
            LastName = "User2",
            SsnHash = "hash2",
            DateOfBirth = new DateTime(1985, 5, 15),
            Phone = "555-0002",
            Email = "test2@example.com",
            Address = new Address
            {
                Street = "456 Test Ave",
                City = "Test Town",
                State = "TS",
                ZipCode = "67890"
            }
        };

        _context.Applicants.AddRange(applicant1, applicant2);

        // Create loan applications
        var application1 = new LoanApplication
        {
            Id = 1,
            AffiliateId = 1,
            ApplicantId = 1,
            ProductType = "Personal Loan",
            Amount = 10000,
            IncomeMonthly = 5000,
            EmploymentType = "Full-time",
            CreditScore = 720,
            Status = ApplicationStatus.Submitted
        };

        var application2 = new LoanApplication
        {
            Id = 2,
            AffiliateId = 2,
            ApplicantId = 2,
            ProductType = "Auto Loan",
            Amount = 25000,
            IncomeMonthly = 7500,
            EmploymentType = "Full-time",
            CreditScore = 680,
            Status = ApplicationStatus.Submitted
        };

        _context.LoanApplications.AddRange(application1, application2);

        // Create documents
        var document1 = new Document
        {
            Id = 1,
            LoanApplicationId = 1,
            FileName = "test.pdf",
            ContentType = "application/pdf",
            StoragePath = "storage/path/test.pdf",
            FileSize = 1024
        };

        var document2 = new Document
        {
            Id = 2,
            LoanApplicationId = 2,
            FileName = "test2.pdf",
            ContentType = "application/pdf",
            StoragePath = "storage/path/test2.pdf",
            FileSize = 2048
        };

        _context.Documents.AddRange(document1, document2);
        _context.SaveChanges();
    }

    private static ClaimsPrincipal CreateClaimsPrincipal(string email, string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new(ClaimTypes.NameIdentifier, "1")
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}