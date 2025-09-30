using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartUnderwrite.Api.Middleware;
using SmartUnderwrite.Api.Services;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Infrastructure.Data;
using System.Security.Claims;
using Xunit;

namespace SmartUnderwrite.Tests.Middleware;

public class AuditMiddlewareTests : IDisposable
{
    private readonly SmartUnderwriteDbContext _context;
    private readonly Mock<ILogger<AuditMiddleware>> _mockLogger;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly AuditMiddleware _middleware;
    private readonly Mock<RequestDelegate> _mockNext;

    public AuditMiddlewareTests()
    {
        var options = new DbContextOptionsBuilder<SmartUnderwriteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SmartUnderwriteDbContext(options);
        _mockLogger = new Mock<ILogger<AuditMiddleware>>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockNext = new Mock<RequestDelegate>();
        
        _middleware = new AuditMiddleware(_mockNext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddCorrelationId_WhenNotPresent()
    {
        // Arrange
        var context = new DefaultHttpContext();
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);
        _mockCurrentUserService.Setup(x => x.GetUserId()).Returns(1);

        // Act
        await _middleware.InvokeAsync(context, _context, _mockCurrentUserService.Object);

        // Assert
        Assert.True(context.Items.ContainsKey("CorrelationId"));
        Assert.NotNull(context.Items["CorrelationId"]);
    }

    [Fact]
    public async Task InvokeAsync_ShouldUseExistingCorrelationId_WhenPresent()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var existingCorrelationId = "existing-correlation-id";
        context.Items["CorrelationId"] = existingCorrelationId;
        
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);
        _mockCurrentUserService.Setup(x => x.GetUserId()).Returns(1);

        // Act
        await _middleware.InvokeAsync(context, _context, _mockCurrentUserService.Object);

        // Assert
        Assert.Equal(existingCorrelationId, context.Items["CorrelationId"]);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCreateAuditLog_WhenEntityIsAdded()
    {
        // Arrange
        var context = new DefaultHttpContext();
        _mockCurrentUserService.Setup(x => x.GetUserId()).Returns(1);
        
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(async (HttpContext ctx) =>
        {
            // Simulate adding an entity during request processing
            var affiliate = new Affiliate
            {
                Name = "Test Affiliate",
                ExternalId = "TEST001",
                IsActive = true
            };
            _context.Affiliates.Add(affiliate);
        });

        // Act
        await _middleware.InvokeAsync(context, _context, _mockCurrentUserService.Object);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Equal("Affiliate", auditLog.EntityType);
        Assert.Equal("CREATE", auditLog.Action);
        Assert.Equal("1", auditLog.UserId);
        Assert.Contains("name", auditLog.Changes.ToLower());
    }

    [Fact]
    public async Task InvokeAsync_ShouldCreateAuditLog_WhenEntityIsModified()
    {
        // Arrange
        var context = new DefaultHttpContext();
        _mockCurrentUserService.Setup(x => x.GetUserId()).Returns(2);

        // Seed an entity first
        var affiliate = new Affiliate
        {
            Name = "Original Name",
            ExternalId = "TEST002",
            IsActive = true
        };
        _context.Affiliates.Add(affiliate);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(async (HttpContext ctx) =>
        {
            // Simulate modifying the entity during request processing
            var existingAffiliate = await _context.Affiliates.FirstAsync();
            existingAffiliate.Name = "Updated Name";
        });

        // Act
        await _middleware.InvokeAsync(context, _context, _mockCurrentUserService.Object);

        // Assert
        var auditLog = await _context.AuditLogs.OrderByDescending(a => a.Timestamp).FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Equal("Affiliate", auditLog.EntityType);
        Assert.Equal("UPDATE", auditLog.Action);
        Assert.Equal("2", auditLog.UserId);
        Assert.Contains("name", auditLog.Changes.ToLower());
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotAuditAuditLogEntity()
    {
        // Arrange
        var context = new DefaultHttpContext();
        _mockCurrentUserService.Setup(x => x.GetUserId()).Returns(1);
        
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(async (HttpContext ctx) =>
        {
            // Simulate adding an audit log during request processing
            var auditLog = new AuditLog
            {
                EntityType = "Test",
                EntityId = "123",
                Action = "CREATE",
                Changes = "{}",
                UserId = "1",
                Timestamp = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);
        });

        // Act
        await _middleware.InvokeAsync(context, _context, _mockCurrentUserService.Object);

        // Assert
        // Should only have the audit log we added, not an audit of the audit log
        var auditLogs = await _context.AuditLogs.ToListAsync();
        Assert.Single(auditLogs);
        Assert.Equal("Test", auditLogs.First().EntityType);
    }

    [Fact]
    public async Task InvokeAsync_ShouldSanitizePiiFields_WhenAuditingApplicant()
    {
        // Arrange
        var context = new DefaultHttpContext();
        _mockCurrentUserService.Setup(x => x.GetUserId()).Returns(1);
        
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(async (HttpContext ctx) =>
        {
            // Simulate adding an applicant with PII
            var applicant = new Applicant
            {
                FirstName = "John",
                LastName = "Doe",
                SsnHash = "hashed-ssn-value",
                DateOfBirth = new DateTime(1990, 1, 1),
                Email = "john.doe@example.com",
                Phone = "555-1234"
            };
            _context.Applicants.Add(applicant);
        });

        // Act
        await _middleware.InvokeAsync(context, _context, _mockCurrentUserService.Object);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Equal("Applicant", auditLog.EntityType);
        
        // PII fields should be sanitized
        Assert.DoesNotContain("John", auditLog.Changes);
        Assert.DoesNotContain("Doe", auditLog.Changes);
        Assert.DoesNotContain("john.doe@example.com", auditLog.Changes);
        Assert.DoesNotContain("555-1234", auditLog.Changes);
        Assert.DoesNotContain("hashed-ssn-value", auditLog.Changes);
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleSystemUser_WhenNoUserIdAvailable()
    {
        // Arrange
        var context = new DefaultHttpContext();
        _mockCurrentUserService.Setup(x => x.GetUserId()).Returns((int?)null);
        
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(async (HttpContext ctx) =>
        {
            var affiliate = new Affiliate
            {
                Name = "System Created",
                ExternalId = "SYS001",
                IsActive = true
            };
            _context.Affiliates.Add(affiliate);
        });

        // Act
        await _middleware.InvokeAsync(context, _context, _mockCurrentUserService.Object);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Null(auditLog.UserId);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}