using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Core.RulesEngine.Models;

namespace SmartUnderwrite.Core.RulesEngine.Interfaces;

public interface IRulesEngine
{
    /// <summary>
    /// Evaluates a loan application against all active rules
    /// </summary>
    /// <param name="application">The loan application to evaluate</param>
    /// <param name="applicant">The applicant information</param>
    /// <returns>Evaluation result with decision, score, and reasons</returns>
    Task<EvaluationResult> EvaluateAsync(LoanApplication application, Applicant applicant);

    /// <summary>
    /// Evaluates a loan application against a specific set of rules
    /// </summary>
    /// <param name="application">The loan application to evaluate</param>
    /// <param name="applicant">The applicant information</param>
    /// <param name="rules">The specific rules to evaluate against</param>
    /// <returns>Evaluation result with decision, score, and reasons</returns>
    Task<EvaluationResult> EvaluateAsync(LoanApplication application, Applicant applicant, IEnumerable<Rule> rules);

    /// <summary>
    /// Validates that a rule definition can be executed successfully
    /// </summary>
    /// <param name="ruleJson">JSON rule definition to validate</param>
    /// <returns>True if the rule can be executed, false otherwise</returns>
    Task<bool> ValidateRuleDefinitionAsync(string ruleJson);

    /// <summary>
    /// Gets all active rules ordered by priority
    /// </summary>
    /// <returns>List of active rules in priority order</returns>
    Task<IEnumerable<Rule>> GetActiveRulesAsync();
}