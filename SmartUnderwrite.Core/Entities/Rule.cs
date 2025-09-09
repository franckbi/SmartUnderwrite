namespace SmartUnderwrite.Core.Entities;

public class Rule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RuleDefinition { get; set; } = string.Empty; // JSON rule definition
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}