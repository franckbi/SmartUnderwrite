using SmartUnderwrite.Core.RulesEngine.Validation;

namespace SmartUnderwrite.Api.Models.Rules;

public class RuleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RuleDefinition { get; set; } = string.Empty;
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RuleDefinition { get; set; } = string.Empty;
    public int Priority { get; set; }
}

public class UpdateRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RuleDefinition { get; set; } = string.Empty;
    public int Priority { get; set; }
}

public class ValidateRuleRequest
{
    public string RuleDefinition { get; set; } = string.Empty;
}

public class RuleValidationResponse
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class RuleVersionDto
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