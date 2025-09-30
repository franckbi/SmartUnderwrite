using SmartUnderwrite.Core.Enums;

namespace SmartUnderwrite.Core.RulesEngine.Models;

public class EvaluationResult
{
    public DecisionOutcome Outcome { get; set; }
    public int Score { get; set; }
    public List<string> Reasons { get; set; } = new();
    public List<RuleExecutionResult> RuleResults { get; set; } = new();
}

public class RuleExecutionResult
{
    public string RuleName { get; set; } = string.Empty;
    public bool Executed { get; set; }
    public DecisionOutcome? Outcome { get; set; }
    public string? Reason { get; set; }
    public int ScoreImpact { get; set; }
    public List<string> Errors { get; set; } = new();
}