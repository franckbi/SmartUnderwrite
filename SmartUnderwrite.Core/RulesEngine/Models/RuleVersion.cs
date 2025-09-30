namespace SmartUnderwrite.Core.RulesEngine.Models;

public class RuleVersion
{
    public int Id { get; set; }
    public int OriginalRuleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RuleDefinition { get; set; } = string.Empty;
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string ChangeReason { get; set; } = string.Empty;
}