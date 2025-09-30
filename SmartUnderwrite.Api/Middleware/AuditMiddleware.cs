using Microsoft.EntityFrameworkCore;
using SmartUnderwrite.Api.Services;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Infrastructure.Data;
using System.Security.Claims;
using System.Text.Json;

namespace SmartUnderwrite.Api.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, SmartUnderwriteDbContext dbContext, ICurrentUserService currentUserService)
    {
        // Add correlation ID to the context if not present
        if (!context.Items.ContainsKey("CorrelationId"))
        {
            context.Items["CorrelationId"] = context.TraceIdentifier ?? Guid.NewGuid().ToString();
        }

        // Set up change tracking before the request
        var originalEntries = new Dictionary<object, EntityState>();
        var originalValues = new Dictionary<object, Dictionary<string, object?>>();

        // Capture original state before processing request
        foreach (var entry in dbContext.ChangeTracker.Entries())
        {
            if (ShouldAuditEntity(entry.Entity))
            {
                originalEntries[entry.Entity] = entry.State;
                
                if (entry.State == EntityState.Modified)
                {
                    originalValues[entry.Entity] = new Dictionary<string, object?>();
                    foreach (var property in entry.OriginalValues.Properties)
                    {
                        originalValues[entry.Entity][property.Name] = entry.OriginalValues[property];
                    }
                }
            }
        }

        await _next(context);

        // Capture changes after request processing
        await CaptureAuditLogs(dbContext, currentUserService, originalEntries, originalValues, context);
    }

    private async Task CaptureAuditLogs(
        SmartUnderwriteDbContext dbContext, 
        ICurrentUserService currentUserService,
        Dictionary<object, EntityState> originalEntries,
        Dictionary<object, Dictionary<string, object?>> originalValues,
        HttpContext context)
    {
        var auditLogs = new List<AuditLog>();
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;
        var userId = currentUserService.GetUserId()?.ToString();

        foreach (var entry in dbContext.ChangeTracker.Entries())
        {
            if (!ShouldAuditEntity(entry.Entity) || entry.State == EntityState.Unchanged)
                continue;

            var entityType = entry.Entity.GetType().Name;
            var entityId = GetEntityId(entry.Entity);
            var action = GetActionFromState(entry.State);

            var changes = new Dictionary<string, object>();

            switch (entry.State)
            {
                case EntityState.Added:
                    changes = GetAddedChanges(entry);
                    break;
                case EntityState.Modified:
                    changes = GetModifiedChanges(entry, originalValues.GetValueOrDefault(entry.Entity));
                    break;
                case EntityState.Deleted:
                    changes = GetDeletedChanges(entry);
                    break;
            }

            // Sanitize PII from changes
            var sanitizedChanges = SanitizePiiFromChanges(changes, entityType);

            var auditLog = new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                Changes = JsonSerializer.Serialize(sanitizedChanges, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                }),
                UserId = userId,
                Timestamp = DateTime.UtcNow
            };

            auditLogs.Add(auditLog);

            _logger.LogInformation("Audit: {Action} {EntityType} {EntityId} by User {UserId} [CorrelationId: {CorrelationId}]",
                action, entityType, entityId, userId ?? "System", correlationId);
        }

        if (auditLogs.Any())
        {
            dbContext.AuditLogs.AddRange(auditLogs);
            await dbContext.SaveChangesAsync();
        }
    }

    private static bool ShouldAuditEntity(object entity)
    {
        // Don't audit the audit logs themselves or other system entities
        return entity is not AuditLog and not Microsoft.AspNetCore.Identity.IdentityUserLogin<int> 
               and not Microsoft.AspNetCore.Identity.IdentityUserToken<int>
               and not Microsoft.AspNetCore.Identity.IdentityUserRole<int>
               and not Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>
               and not Microsoft.AspNetCore.Identity.IdentityUserClaim<int>;
    }

    private static string GetEntityId(object entity)
    {
        var idProperty = entity.GetType().GetProperty("Id");
        return idProperty?.GetValue(entity)?.ToString() ?? "Unknown";
    }

    private static string GetActionFromState(EntityState state)
    {
        return state switch
        {
            EntityState.Added => "CREATE",
            EntityState.Modified => "UPDATE",
            EntityState.Deleted => "DELETE",
            _ => "UNKNOWN"
        };
    }

    private static Dictionary<string, object> GetAddedChanges(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var changes = new Dictionary<string, object>();
        
        foreach (var property in entry.CurrentValues.Properties)
        {
            var currentValue = entry.CurrentValues[property];
            if (currentValue != null)
            {
                changes[property.Name] = currentValue;
            }
        }

        return changes;
    }

    private static Dictionary<string, object> GetModifiedChanges(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, 
        Dictionary<string, object?>? originalValues)
    {
        var changes = new Dictionary<string, object>();

        foreach (var property in entry.Properties.Where(p => p.IsModified))
        {
            var propertyName = property.Metadata.Name;
            var originalValue = originalValues?.GetValueOrDefault(propertyName);
            var currentValue = property.CurrentValue;

            changes[propertyName] = new
            {
                From = originalValue,
                To = currentValue
            };
        }

        return changes;
    }

    private static Dictionary<string, object> GetDeletedChanges(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var changes = new Dictionary<string, object>();
        
        foreach (var property in entry.OriginalValues.Properties)
        {
            var originalValue = entry.OriginalValues[property];
            if (originalValue != null)
            {
                changes[property.Name] = originalValue;
            }
        }

        return changes;
    }

    private static Dictionary<string, object> SanitizePiiFromChanges(Dictionary<string, object> changes, string entityType)
    {
        var sanitizedChanges = new Dictionary<string, object>();

        foreach (var change in changes)
        {
            var key = change.Key;
            var value = change.Value;

            // Sanitize PII fields based on entity type and property name
            if (IsPiiField(entityType, key))
            {
                sanitizedChanges[key] = SanitizeValue(value);
            }
            else
            {
                sanitizedChanges[key] = value;
            }
        }

        return sanitizedChanges;
    }

    private static bool IsPiiField(string entityType, string propertyName)
    {
        var piiFields = new Dictionary<string, HashSet<string>>
        {
            ["Applicant"] = new HashSet<string> { "SsnHash", "Email", "Phone", "FirstName", "LastName", "DateOfBirth" },
            ["User"] = new HashSet<string> { "Email", "FirstName", "LastName", "PhoneNumber" },
            ["Address"] = new HashSet<string> { "Street", "City", "State", "ZipCode" }
        };

        return piiFields.ContainsKey(entityType) && piiFields[entityType].Contains(propertyName);
    }

    private static object SanitizeValue(object? value)
    {
        if (value == null) return "null";
        
        var stringValue = value.ToString();
        if (string.IsNullOrEmpty(stringValue)) return "[EMPTY]";
        
        // For complex objects (like change tracking objects), handle them specially
        if (value.GetType().IsAnonymousType())
        {
            return "[PII_REDACTED]";
        }
        
        // For simple values, mask them
        return stringValue.Length <= 4 ? "[REDACTED]" : $"{stringValue[..2]}***{stringValue[^2..]}";
    }
}

// Extension method to check if type is anonymous
public static class TypeExtensions
{
    public static bool IsAnonymousType(this Type type)
    {
        return type.Name.Contains("AnonymousType", StringComparison.OrdinalIgnoreCase);
    }
}