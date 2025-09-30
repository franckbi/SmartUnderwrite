using SmartUnderwrite.Core.RulesEngine.Models;

namespace SmartUnderwrite.Core.RulesEngine.Interfaces;

public interface IRuleVersionRepository
{
    /// <summary>
    /// Gets the version history of a rule
    /// </summary>
    /// <param name="originalRuleId">Original rule ID</param>
    /// <returns>List of rule versions ordered by version number</returns>
    Task<IEnumerable<RuleVersion>> GetRuleHistoryAsync(int originalRuleId);

    /// <summary>
    /// Gets the latest version record for a rule
    /// </summary>
    /// <param name="originalRuleId">Original rule ID</param>
    /// <returns>The latest version record, or null if none exists</returns>
    Task<RuleVersion?> GetLatestVersionAsync(int originalRuleId);

    /// <summary>
    /// Creates a new version record
    /// </summary>
    /// <param name="ruleVersion">The version record to create</param>
    /// <returns>The created version record with assigned ID</returns>
    Task<RuleVersion> CreateAsync(RuleVersion ruleVersion);

    /// <summary>
    /// Gets a specific version record by ID
    /// </summary>
    /// <param name="id">Version record ID</param>
    /// <returns>The version record if found, null otherwise</returns>
    Task<RuleVersion?> GetByIdAsync(int id);

    /// <summary>
    /// Gets all version records
    /// </summary>
    /// <returns>List of all version records</returns>
    Task<IEnumerable<RuleVersion>> GetAllAsync();
}