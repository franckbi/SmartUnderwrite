using SmartUnderwrite.Core.Entities;

namespace SmartUnderwrite.Core.RulesEngine.Interfaces;

public interface IRuleRepository
{
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
    Task<Rule?> GetByIdAsync(int id);

    /// <summary>
    /// Gets all rules (active and inactive)
    /// </summary>
    /// <returns>List of all rules</returns>
    Task<IEnumerable<Rule>> GetAllAsync();

    /// <summary>
    /// Creates a new rule
    /// </summary>
    /// <param name="rule">The rule to create</param>
    /// <returns>The created rule with assigned ID</returns>
    Task<Rule> CreateAsync(Rule rule);

    /// <summary>
    /// Updates an existing rule
    /// </summary>
    /// <param name="rule">The rule to update</param>
    /// <returns>The updated rule</returns>
    Task<Rule> UpdateAsync(Rule rule);

    /// <summary>
    /// Deletes a rule
    /// </summary>
    /// <param name="id">The ID of the rule to delete</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(int id);
}