using System.Text.Json.Serialization;

namespace SmartUnderwrite.Core.RulesEngine.Models;

public class RuleDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("clauses")]
    public List<RuleClause> Clauses { get; set; } = new();

    [JsonPropertyName("score")]
    public ScoreDefinition? Score { get; set; }
}

public class RuleClause
{
    [JsonPropertyName("if")]
    public string Condition { get; set; } = string.Empty;

    [JsonPropertyName("then")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

public class ScoreDefinition
{
    [JsonPropertyName("base")]
    public int Base { get; set; }

    [JsonPropertyName("add")]
    public List<ScoreModifier> Add { get; set; } = new();

    [JsonPropertyName("subtract")]
    public List<ScoreModifier> Subtract { get; set; } = new();
}

public class ScoreModifier
{
    [JsonPropertyName("when")]
    public string Condition { get; set; } = string.Empty;

    [JsonPropertyName("points")]
    public int Points { get; set; }
}