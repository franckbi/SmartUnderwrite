using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Core.RulesEngine.Models;
using SmartUnderwrite.Core.RulesEngine.Validation;

namespace SmartUnderwrite.Core.RulesEngine.Interfaces;

public interface IRuleService
{
    /// <summary>
    /// Gets all rules (active and inactive)
    /// </summary>
    /// <returns>List of all rules</returns>
    Task<IEnumerable<Rule>> GetAllRulesAsync();

    /// <summary>
    /// Gets all active rules ordered by priority
    /// </summary>
    /// <returns>List of active rules in priority order</returns>
    Task<IEnumerable<Rule>> GetActiveRulesAsync();

    /// <summary>
    /// Gets a rule by its ID
    /// </summary>
    /// <param name="id">The rule ID</param>
    /// <returns>The rule if found, null otherwise</returns>
    Task<Rule?> GetRuleByIdAsync(int id);

    /// <summary>
    /// Creates a new rule with validation
    /// </summary>
    /// <param name="name">Rule name</param>
    /// <param name="description">Rule description</param>
    /// <param name="ruleDefinition">JSON rule definition</param>
    /// <param name="priority">Rule priority</param>
    /// <returns>The created rule</returns>
    /// <exception cref="ArgumentException">Thrown when rule definition is invalid</exception>
    Task<Rule> CreateRuleAsync(string name, string description, string ruleDefinition, int priority);

    /// <summary>
    /// Updates an existing rule with validation
    /// </summary>
    /// <param name="id">Rule ID to update</param>
    /// <param name="name">Updated rule name</param>
    /// <param name="description">Updated rule description</param>
    /// <param name="ruleDefinition">Updated JSON rule definition</param>
    /// <param name="priority">Updated rule priority</param>
    /// <returns>The updated rule</returns>
    /// <exception cref="ArgumentException">Thrown when rule definition is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when rule is not found</exception>
    Task<Rule> UpdateRuleAsync(int id, string name, string description, string ruleDefinition, int priority);

    /// <summary>
    /// Activates a rule
    /// </summary>
    /// <param name="id">Rule ID to activate</param>
    /// <returns>The updated rule</returns>
    /// <exception cref="InvalidOperationException">Thrown when rule is not found</exception>
    Task<Rule> ActivateRuleAsync(int id);

    /// <summary>
    /// Deactivates a rule
    /// </summary>
    /// <param name="id">Rule ID to deactivate</param>
    /// <returns>The updated rule</returns>
    /// <exception cref="InvalidOperationException">Thrown when rule is not found</exception>
    Task<Rule> DeactivateRuleAsync(int id);

    /// <summary>
    /// Deletes a rule permanently
    /// </summary>
    /// <param name="id">Rule ID to delete</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteRuleAsync(int id);

    /// <summary>
    /// Validates a rule definition without saving
    /// </summary>
    /// <param name="ruleDefinition">JSON rule definition to validate</param>
    /// <returns>Validation result with errors and warnings</returns>
    Task<RuleValidationResult> ValidateRuleDefinitionAsync(string ruleDefinition);

    /// <summary>
    /// Gets the version history of a rule
    /// </summary>
    /// <param name="id">Rule ID</param>
    /// <returns>List of rule versions ordered by creation date</returns>
    Task<IEnumerable<RuleVersion>> GetRuleHistoryAsync(int id);

    /// <summary>
    /// Creates a new version of an existing rule
    /// </summary>
    /// <param name="id">Original rule ID</param>
    /// <param name="name">Updated rule name</param>
    /// <param name="description">Updated rule description</param>
    /// <param name="ruleDefinition">Updated JSON rule definition</param>
    /// <param name="priority">Updated rule priority</param>
    /// <returns>The new rule version</returns>
    Task<Rule> CreateRuleVersionAsync(int id, string name, string description, string ruleDefinition, int priority);
}