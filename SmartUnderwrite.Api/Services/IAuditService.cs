using SmartUnderwrite.Core.Entities;

namespace SmartUnderwrite.Api.Services;

public interface IAuditService
{
    Task LogAsync(string entityType, string entityId, string action, object changes, int? userId = null);
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(string? entityType = null, string? entityId = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<AuditLog>> GetEntityAuditTrailAsync(string entityType, string entityId);
    Task<AuditLog?> GetAuditLogAsync(int id);
}