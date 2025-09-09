namespace SmartUnderwrite.Core.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE
    public string Changes { get; set; } = string.Empty; // JSON representation of changes
    public string? UserId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}