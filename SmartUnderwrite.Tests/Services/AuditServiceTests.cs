using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartUnderwrite.Api.Services;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Infrastructure.Data;
using Xunit;

namespace SmartUnderwrite.Tests.Services;

public class AuditServiceTests : IDisposable
{
    private readonly SmartUnderwriteDbContext _context;
    private readonly Mock<ILogger<AuditService>> _mockLogger;
    private readonly AuditService _auditService;

    public AuditServiceTests()
    {
        var options = new DbContextOptionsBuilder<SmartUnderwriteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SmartUnderwriteDbContext(options);
        _mockLogger = new Mock<ILogger<AuditService>>();
        _auditService = new AuditService(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task LogAsync_ShouldCreateAuditLog_WhenValidDataProvided()
    {
        // Arrange
        var entityType = "LoanApplication";
        var entityId = "123";
        var action = "CREATE";
        var changes = new { Amount = 10000, Status = "Submitted" };
        var userId = 1;

        // Act
        await _auditService.LogAsync(entityType, entityId, action, changes, userId);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Equal(entityType, auditLog.EntityType);
        Assert.Equal(entityId, auditLog.EntityId);
        Assert.Equal(action, auditLog.Action);
        Assert.Equal(userId.ToString(), auditLog.UserId);
        Assert.Contains("amount", auditLog.Changes.ToLower());
        Assert.Contains("10000", auditLog.Changes);
    }

    [Fact]
    public async Task LogAsync_ShouldCreateAuditLogWithoutUserId_WhenUserIdIsNull()
    {
        // Arrange
        var entityType = "Rule";
        var entityId = "456";
        var action = "UPDATE";
        var changes = new { Name = "Updated Rule", IsActive = true };

        // Act
        await _auditService.LogAsync(entityType, entityId, action, changes);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Equal(entityType, auditLog.EntityType);
        Assert.Equal(entityId, auditLog.EntityId);
        Assert.Equal(action, auditLog.Action);
        Assert.Null(auditLog.UserId);
    }

    [Fact]
    public async Task GetAuditLogsAsync_ShouldReturnAllLogs_WhenNoFiltersProvided()
    {
        // Arrange
        await SeedAuditLogs();

        // Act
        var result = await _auditService.GetAuditLogsAsync();

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetAuditLogsAsync_ShouldFilterByEntityType_WhenEntityTypeProvided()
    {
        // Arrange
        await SeedAuditLogs();

        // Act
        var result = await _auditService.GetAuditLogsAsync(entityType: "LoanApplication");

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, log => Assert.Equal("LoanApplication", log.EntityType));
    }

    [Fact]
    public async Task GetAuditLogsAsync_ShouldFilterByEntityId_WhenEntityIdProvided()
    {
        // Arrange
        await SeedAuditLogs();

        // Act
        var result = await _auditService.GetAuditLogsAsync(entityId: "123");

        // Assert
        Assert.Single(result);
        Assert.Equal("123", result.First().EntityId);
    }

    [Fact]
    public async Task GetAuditLogsAsync_ShouldFilterByDateRange_WhenDatesProvided()
    {
        // Arrange
        await SeedAuditLogs();
        var fromDate = DateTime.UtcNow.AddHours(-1);
        var toDate = DateTime.UtcNow.AddHours(1);

        // Act
        var result = await _auditService.GetAuditLogsAsync(fromDate: fromDate, toDate: toDate);

        // Assert
        Assert.Equal(3, result.Count());
        Assert.All(result, log => Assert.True(log.Timestamp >= fromDate && log.Timestamp <= toDate));
    }

    [Fact]
    public async Task GetEntityAuditTrailAsync_ShouldReturnOrderedTrail_WhenEntityExists()
    {
        // Arrange
        await SeedAuditLogs();

        // Act
        var result = await _auditService.GetEntityAuditTrailAsync("LoanApplication", "123");

        // Assert
        Assert.Single(result);
        Assert.Equal("LoanApplication", result.First().EntityType);
        Assert.Equal("123", result.First().EntityId);
    }

    [Fact]
    public async Task GetEntityAuditTrailAsync_ShouldReturnEmpty_WhenEntityNotExists()
    {
        // Arrange
        await SeedAuditLogs();

        // Act
        var result = await _auditService.GetEntityAuditTrailAsync("NonExistent", "999");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAuditLogAsync_ShouldReturnLog_WhenIdExists()
    {
        // Arrange
        await SeedAuditLogs();
        var existingLog = await _context.AuditLogs.FirstAsync();

        // Act
        var result = await _auditService.GetAuditLogAsync(existingLog.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingLog.Id, result.Id);
        Assert.Equal(existingLog.EntityType, result.EntityType);
    }

    [Fact]
    public async Task GetAuditLogAsync_ShouldReturnNull_WhenIdNotExists()
    {
        // Arrange
        await SeedAuditLogs();

        // Act
        var result = await _auditService.GetAuditLogAsync(999);

        // Assert
        Assert.Null(result);
    }

    private async Task SeedAuditLogs()
    {
        var auditLogs = new[]
        {
            new AuditLog
            {
                EntityType = "LoanApplication",
                EntityId = "123",
                Action = "CREATE",
                Changes = "{\"amount\": 10000, \"status\": \"Submitted\"}",
                UserId = "1",
                Timestamp = DateTime.UtcNow.AddMinutes(-10)
            },
            new AuditLog
            {
                EntityType = "LoanApplication",
                EntityId = "456",
                Action = "UPDATE",
                Changes = "{\"status\": \"Approved\"}",
                UserId = "2",
                Timestamp = DateTime.UtcNow.AddMinutes(-5)
            },
            new AuditLog
            {
                EntityType = "Rule",
                EntityId = "789",
                Action = "DELETE",
                Changes = "{\"name\": \"Old Rule\"}",
                UserId = "1",
                Timestamp = DateTime.UtcNow.AddMinutes(-2)
            }
        };

        _context.AuditLogs.AddRange(auditLogs);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAuditLogsAsync_ShouldLimitResults_WhenTooManyLogsExist()
    {
        // Arrange
        var auditLogs = new List<AuditLog>();
        for (int i = 0; i < 1500; i++) // More than the 1000 limit
        {
            auditLogs.Add(new AuditLog
            {
                EntityType = "TestEntity",
                EntityId = i.ToString(),
                Action = "CREATE",
                Changes = "{}",
                UserId = "1",
                Timestamp = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        _context.AuditLogs.AddRange(auditLogs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _auditService.GetAuditLogsAsync();

        // Assert
        Assert.Equal(1000, result.Count()); // Should be limited to 1000
    }

    [Fact]
    public async Task GetAuditLogsAsync_ShouldReturnOrderedByTimestamp_WhenMultipleLogsExist()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var auditLogs = new[]
        {
            new AuditLog
            {
                EntityType = "Test",
                EntityId = "1",
                Action = "CREATE",
                Changes = "{}",
                UserId = "1",
                Timestamp = now.AddMinutes(-10)
            },
            new AuditLog
            {
                EntityType = "Test",
                EntityId = "2",
                Action = "UPDATE",
                Changes = "{}",
                UserId = "1",
                Timestamp = now.AddMinutes(-5)
            },
            new AuditLog
            {
                EntityType = "Test",
                EntityId = "3",
                Action = "DELETE",
                Changes = "{}",
                UserId = "1",
                Timestamp = now.AddMinutes(-1)
            }
        };

        _context.AuditLogs.AddRange(auditLogs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _auditService.GetAuditLogsAsync();

        // Assert
        var resultList = result.ToList();
        Assert.Equal(3, resultList.Count);
        
        // Should be ordered by timestamp descending (most recent first)
        Assert.True(resultList[0].Timestamp > resultList[1].Timestamp);
        Assert.True(resultList[1].Timestamp > resultList[2].Timestamp);
        Assert.Equal("3", resultList[0].EntityId); // Most recent
        Assert.Equal("1", resultList[2].EntityId); // Oldest
    }

    [Fact]
    public async Task GetEntityAuditTrailAsync_ShouldReturnOrderedByTimestamp_WhenMultipleEntriesExist()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var auditLogs = new[]
        {
            new AuditLog
            {
                EntityType = "LoanApplication",
                EntityId = "123",
                Action = "CREATE",
                Changes = "{}",
                UserId = "1",
                Timestamp = now.AddMinutes(-10)
            },
            new AuditLog
            {
                EntityType = "LoanApplication",
                EntityId = "123",
                Action = "UPDATE",
                Changes = "{}",
                UserId = "2",
                Timestamp = now.AddMinutes(-5)
            },
            new AuditLog
            {
                EntityType = "LoanApplication",
                EntityId = "123",
                Action = "UPDATE",
                Changes = "{}",
                UserId = "1",
                Timestamp = now.AddMinutes(-1)
            }
        };

        _context.AuditLogs.AddRange(auditLogs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _auditService.GetEntityAuditTrailAsync("LoanApplication", "123");

        // Assert
        var resultList = result.ToList();
        Assert.Equal(3, resultList.Count);
        
        // Should be ordered by timestamp ascending (chronological order)
        Assert.True(resultList[0].Timestamp < resultList[1].Timestamp);
        Assert.True(resultList[1].Timestamp < resultList[2].Timestamp);
        Assert.Equal("CREATE", resultList[0].Action); // First action
        Assert.Equal("UPDATE", resultList[2].Action); // Last action
    }

    [Theory]
    [InlineData("LoanApplication", "123")]
    [InlineData("Rule", "456")]
    [InlineData("User", "789")]
    public async Task GetEntityAuditTrailAsync_ShouldFilterCorrectly_ForDifferentEntityTypes(string entityType, string entityId)
    {
        // Arrange
        var auditLogs = new[]
        {
            new AuditLog { EntityType = "LoanApplication", EntityId = "123", Action = "CREATE", Changes = "{}", Timestamp = DateTime.UtcNow },
            new AuditLog { EntityType = "Rule", EntityId = "456", Action = "UPDATE", Changes = "{}", Timestamp = DateTime.UtcNow },
            new AuditLog { EntityType = "User", EntityId = "789", Action = "DELETE", Changes = "{}", Timestamp = DateTime.UtcNow },
            new AuditLog { EntityType = "LoanApplication", EntityId = "999", Action = "CREATE", Changes = "{}", Timestamp = DateTime.UtcNow }
        };

        _context.AuditLogs.AddRange(auditLogs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _auditService.GetEntityAuditTrailAsync(entityType, entityId);

        // Assert
        Assert.Single(result);
        Assert.Equal(entityType, result.First().EntityType);
        Assert.Equal(entityId, result.First().EntityId);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}