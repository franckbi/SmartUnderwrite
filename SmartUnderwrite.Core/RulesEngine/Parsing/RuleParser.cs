using System.Text.Json;
using SmartUnderwrite.Core.RulesEngine.Interfaces;
using SmartUnderwrite.Core.RulesEngine.Models;
using SmartUnderwrite.Core.RulesEngine.Validation;

namespace SmartUnderwrite.Core.RulesEngine.Parsing;

public class RuleParser : IRuleParser
{
    private readonly IExpressionCompiler _expressionCompiler;
    private readonly JsonSerializerOptions _jsonOptions;

    public RuleParser(IExpressionCompiler expressionCompiler)
    {
        _expressionCompiler = expressionCompiler ?? throw new ArgumentNullException(nameof(expressionCompiler));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
    }

    public RuleDefinition ParseRuleDefinition(string ruleJson)
    {
        if (string.IsNullOrWhiteSpace(ruleJson))
            throw new ArgumentException("Rule JSON cannot be null or empty", nameof(ruleJson));

        try
        {
            var ruleDefinition = JsonSerializer.Deserialize<RuleDefinition>(ruleJson, _jsonOptions);
            if (ruleDefinition == null)
                throw new ArgumentException("Failed to deserialize rule definition");

            return ruleDefinition;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON format: {ex.Message}", nameof(ruleJson), ex);
        }
    }

    public RuleValidationResult ValidateRuleJson(string ruleJson)
    {
        var result = new RuleValidationResult { IsValid = true };

        if (string.IsNullOrWhiteSpace(ruleJson))
        {
            result.AddError("Rule JSON cannot be null or empty");
            return result;
        }

        try
        {
            var ruleDefinition = ParseRuleDefinition(ruleJson);
            return ValidateRuleDefinition(ruleDefinition);
        }
        catch (ArgumentException ex)
        {
            result.AddError($"JSON parsing error: {ex.Message}");
            return result;
        }
    }

    public RuleValidationResult ValidateRuleDefinition(RuleDefinition ruleDefinition)
    {
        var result = new RuleValidationResult { IsValid = true };

        if (ruleDefinition == null)
        {
            result.AddError("Rule definition cannot be null");
            return result;
        }

        // Validate basic properties
        ValidateBasicProperties(ruleDefinition, result);

        // Validate clauses
        ValidateClauses(ruleDefinition.Clauses, result);

        // Validate score definition
        if (ruleDefinition.Score != null)
        {
            ValidateScoreDefinition(ruleDefinition.Score, result);
        }

        return result;
    }

    private void ValidateBasicProperties(RuleDefinition ruleDefinition, RuleValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(ruleDefinition.Name))
        {
            result.AddError("Rule name is required");
        }

        if (ruleDefinition.Priority < 0)
        {
            result.AddError("Rule priority must be non-negative");
        }

        if (ruleDefinition.Clauses == null || ruleDefinition.Clauses.Count == 0)
        {
            result.AddError("Rule must have at least one clause");
        }
    }

    private void ValidateClauses(List<RuleClause> clauses, RuleValidationResult result)
    {
        if (clauses == null) return;

        for (int i = 0; i < clauses.Count; i++)
        {
            var clause = clauses[i];
            var clausePrefix = $"Clause {i + 1}";

            // Validate condition
            if (string.IsNullOrWhiteSpace(clause.Condition))
            {
                result.AddError($"{clausePrefix}: Condition is required");
            }
            else if (!_expressionCompiler.ValidateCondition(clause.Condition))
            {
                result.AddError($"{clausePrefix}: Invalid condition syntax '{clause.Condition}'");
            }

            // Validate action
            if (string.IsNullOrWhiteSpace(clause.Action))
            {
                result.AddError($"{clausePrefix}: Action is required");
            }
            else if (!IsValidAction(clause.Action))
            {
                result.AddError($"{clausePrefix}: Invalid action '{clause.Action}'. Must be APPROVE, REJECT, or MANUAL");
            }

            // Validate reason
            if (string.IsNullOrWhiteSpace(clause.Reason))
            {
                result.AddWarning($"{clausePrefix}: Reason is recommended for better decision transparency");
            }
        }
    }

    private void ValidateScoreDefinition(ScoreDefinition scoreDefinition, RuleValidationResult result)
    {
        if (scoreDefinition.Base < 0)
        {
            result.AddError("Base score must be non-negative");
        }

        // Validate score modifiers
        ValidateScoreModifiers(scoreDefinition.Add, "Add", result);
        ValidateScoreModifiers(scoreDefinition.Subtract, "Subtract", result);
    }

    private void ValidateScoreModifiers(List<ScoreModifier> modifiers, string type, RuleValidationResult result)
    {
        if (modifiers == null) return;

        for (int i = 0; i < modifiers.Count; i++)
        {
            var modifier = modifiers[i];
            var modifierPrefix = $"{type} modifier {i + 1}";

            if (string.IsNullOrWhiteSpace(modifier.Condition))
            {
                result.AddError($"{modifierPrefix}: Condition is required");
            }
            else if (!_expressionCompiler.ValidateCondition(modifier.Condition))
            {
                result.AddError($"{modifierPrefix}: Invalid condition syntax '{modifier.Condition}'");
            }

            if (modifier.Points < 0)
            {
                result.AddError($"{modifierPrefix}: Points must be non-negative");
            }

            if (modifier.Points == 0)
            {
                result.AddWarning($"{modifierPrefix}: Zero points modifier has no effect");
            }
        }
    }

    private bool IsValidAction(string action)
    {
        var validActions = new[] { "APPROVE", "REJECT", "MANUAL" };
        return validActions.Contains(action.ToUpperInvariant());
    }
}