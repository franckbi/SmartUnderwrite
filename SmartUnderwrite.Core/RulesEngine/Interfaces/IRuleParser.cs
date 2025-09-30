using SmartUnderwrite.Core.RulesEngine.Models;
using SmartUnderwrite.Core.RulesEngine.Validation;

namespace SmartUnderwrite.Core.RulesEngine.Interfaces;

public interface IRuleParser
{
    /// <summary>
    /// Parses a JSON rule definition into a strongly-typed RuleDefinition object
    /// </summary>
    /// <param name="ruleJson">JSON string containing the rule definition</param>
    /// <returns>Parsed RuleDefinition object</returns>
    /// <exception cref="ArgumentException">Thrown when JSON is invalid or malformed</exception>
    RuleDefinition ParseRuleDefinition(string ruleJson);

    /// <summary>
    /// Validates a rule definition for syntax and semantic correctness
    /// </summary>
    /// <param name="ruleDefinition">The rule definition to validate</param>
    /// <returns>Validation result with errors and warnings</returns>
    RuleValidationResult ValidateRuleDefinition(RuleDefinition ruleDefinition);

    /// <summary>
    /// Validates a JSON rule definition string
    /// </summary>
    /// <param name="ruleJson">JSON string containing the rule definition</param>
    /// <returns>Validation result with errors and warnings</returns>
    RuleValidationResult ValidateRuleJson(string ruleJson);
}