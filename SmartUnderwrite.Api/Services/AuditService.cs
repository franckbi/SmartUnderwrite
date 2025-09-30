using Microsoft.EntityFrameworkCore;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Infrastructure.Data;
using System.Text.Json;

namespace SmartUnderwrite.Api.Services;

public class AuditService : IAuditService
{
    private readonly SmartUnderwriteDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(SmartUnderwriteDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(string entityType, string entityId, string action, object changes, int? userId = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                Changes = JsonSerializer.Serialize(changes, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                }),
                UserId = userId?.ToString(),
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Manual audit log created: {Action} {EntityType} {EntityId} by User {UserId}",
                action, entityType, entityId, userId?.ToString() ?? "System");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for {EntityType} {EntityId}", entityType, entityId);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(
        string? entityType = null, 
        string? entityId = null, 
        DateTime? fromDate = null, 
        DateTime? toDate = null)
    {
        try
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(entityType))
            {
                query = query.Where(a => a.EntityType == entityType);
            }

            if (!string.IsNullOrEmpty(entityId))
            {
                query = query.Where(a => a.EntityId == entityId);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= toDate.Value);
            }

            var results = await query
                .OrderByDescending(a => a.Timestamp)
                .Take(1000) // Limit results for performance
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} audit logs with filters: EntityType={EntityType}, EntityId={EntityId}, FromDate={FromDate}, ToDate={ToDate}",
                results.Count, entityType, entityId, fromDate, toDate);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve audit logs");
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetEntityAuditTrailAsync(string entityType, string entityId)
    {
        try
        {
            var auditTrail = await _context.AuditLogs
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .OrderBy(a => a.Timestamp)
                .ToListAsync();

            _logger.LogDebug("Retrieved audit trail for {EntityType} {EntityId}: {Count} entries",
                entityType, entityId, auditTrail.Count);

            return auditTrail;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve audit trail for {EntityType} {EntityId}", entityType, entityId);
            throw;
        }
    }

    public async Task<AuditLog?> GetAuditLogAsync(int id)
    {
        try
        {
            var auditLog = await _context.AuditLogs
                .FirstOrDefaultAsync(a => a.Id == id);

            if (auditLog != null)
            {
                _logger.LogDebug("Retrieved audit log {Id}", id);
            }
            else
            {
                _logger.LogWarning("Audit log {Id} not found", id);
            }

            return auditLog;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve audit log {Id}", id);
            throw;
        }
    }
}