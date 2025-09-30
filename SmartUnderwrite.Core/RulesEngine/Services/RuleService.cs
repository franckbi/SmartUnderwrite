using Microsoft.Extensions.Logging;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Core.RulesEngine.Interfaces;
using SmartUnderwrite.Core.RulesEngine.Models;
using SmartUnderwrite.Core.RulesEngine.Validation;

namespace SmartUnderwrite.Core.RulesEngine.Services;

public class RuleService : IRuleService
{
    private readonly IRuleRepository _ruleRepository;
    private readonly IRuleVersionRepository _ruleVersionRepository;
    private readonly IRuleParser _ruleParser;
    private readonly ILogger<RuleService> _logger;

    public RuleService(
        IRuleRepository ruleRepository,
        IRuleVersionRepository ruleVersionRepository,
        IRuleParser ruleParser,
        ILogger<RuleService> logger)
    {
        _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
        _ruleVersionRepository = ruleVersionRepository ?? throw new ArgumentNullException(nameof(ruleVersionRepository));
        _ruleParser = ruleParser ?? throw new ArgumentNullException(nameof(ruleParser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<Rule>> GetAllRulesAsync()
    {
        _logger.LogDebug("Getting all rules");
        return await _ruleRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Rule>> GetActiveRulesAsync()
    {
        _logger.LogDebug("Getting active rules");
        return await _ruleRepository.GetActiveRulesAsync();
    }

    public async Task<Rule?> GetRuleByIdAsync(int id)
    {
        _logger.LogDebug("Getting rule by ID: {RuleId}", id);
        return await _ruleRepository.GetByIdAsync(id);
    }

    public async Task<Rule> CreateRuleAsync(string name, string description, string ruleDefinition, int priority)
    {
        _logger.LogInformation("Creating new rule: {RuleName}", name);

        // Validate the rule definition
        var validationResult = await ValidateRuleDefinitionAsync(ruleDefinition);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors);
            throw new ArgumentException($"Invalid rule definition: {errors}");
        }

        var rule = new Rule
        {
            Name = name,
            Description = description,
            RuleDefinition = ruleDefinition,
            Priority = priority,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createdRule = await _ruleRepository.CreateAsync(rule);

        // Create initial version record
        await CreateVersionRecord(createdRule, "Initial version");

        _logger.LogInformation("Created rule {RuleId}: {RuleName}", createdRule.Id, createdRule.Name);
        return createdRule;
    }

    public async Task<Rule> UpdateRuleAsync(int id, string name, string description, string ruleDefinition, int priority)
    {
        _logger.LogInformation("Updating rule {RuleId}: {RuleName}", id, name);

        var existingRule = await _ruleRepository.GetByIdAsync(id);
        if (existingRule == null)
        {
            throw new InvalidOperationException($"Rule with ID {id} not found");
        }

        // Validate the rule definition
        var validationResult = await ValidateRuleDefinitionAsync(ruleDefinition);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors);
            throw new ArgumentException($"Invalid rule definition: {errors}");
        }

        // Create version record before updating
        await CreateVersionRecord(existingRule, "Rule updated");

        // Update the rule
        existingRule.Name = name;
        existingRule.Description = description;
        existingRule.RuleDefinition = ruleDefinition;
        existingRule.Priority = priority;
        existingRule.UpdatedAt = DateTime.UtcNow;

        var updatedRule = await _ruleRepository.UpdateAsync(existingRule);

        _logger.LogInformation("Updated rule {RuleId}: {RuleName}", updatedRule.Id, updatedRule.Name);
        return updatedRule;
    }

    public async Task<Rule> ActivateRuleAsync(int id)
    {
        _logger.LogInformation("Activating rule {RuleId}", id);

        var rule = await _ruleRepository.GetByIdAsync(id);
        if (rule == null)
        {
            throw new InvalidOperationException($"Rule with ID {id} not found");
        }

        if (rule.IsActive)
        {
            _logger.LogWarning("Rule {RuleId} is already active", id);
            return rule;
        }

        // Create version record
        await CreateVersionRecord(rule, "Rule activated");

        rule.IsActive = true;
        rule.UpdatedAt = DateTime.UtcNow;

        var updatedRule = await _ruleRepository.UpdateAsync(rule);

        _logger.LogInformation("Activated rule {RuleId}: {RuleName}", updatedRule.Id, updatedRule.Name);
        return updatedRule;
    }

    public async Task<Rule> DeactivateRuleAsync(int id)
    {
        _logger.LogInformation("Deactivating rule {RuleId}", id);

        var rule = await _ruleRepository.GetByIdAsync(id);
        if (rule == null)
        {
            throw new InvalidOperationException($"Rule with ID {id} not found");
        }

        if (!rule.IsActive)
        {
            _logger.LogWarning("Rule {RuleId} is already inactive", id);
            return rule;
        }

        // Create version record
        await CreateVersionRecord(rule, "Rule deactivated");

        rule.IsActive = false;
        rule.UpdatedAt = DateTime.UtcNow;

        var updatedRule = await _ruleRepository.UpdateAsync(rule);

        _logger.LogInformation("Deactivated rule {RuleId}: {RuleName}", updatedRule.Id, updatedRule.Name);
        return updatedRule;
    }

    public async Task<bool> DeleteRuleAsync(int id)
    {
        _logger.LogInformation("Deleting rule {RuleId}", id);

        var rule = await _ruleRepository.GetByIdAsync(id);
        if (rule == null)
        {
            _logger.LogWarning("Rule {RuleId} not found for deletion", id);
            return false;
        }

        // Create final version record
        await CreateVersionRecord(rule, "Rule deleted");

        var deleted = await _ruleRepository.DeleteAsync(id);

        if (deleted)
        {
            _logger.LogInformation("Deleted rule {RuleId}: {RuleName}", id, rule.Name);
        }

        return deleted;
    }

    public async Task<RuleValidationResult> ValidateRuleDefinitionAsync(string ruleDefinition)
    {
        _logger.LogDebug("Validating rule definition");

        try
        {
            return _ruleParser.ValidateRuleJson(ruleDefinition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating rule definition");
            return RuleValidationResult.Failure($"Validation error: {ex.Message}");
        }
    }

    public async Task<IEnumerable<RuleVersion>> GetRuleHistoryAsync(int id)
    {
        _logger.LogDebug("Getting rule history for rule {RuleId}", id);
        return await _ruleVersionRepository.GetRuleHistoryAsync(id);
    }

    public async Task<Rule> CreateRuleVersionAsync(int id, string name, string description, string ruleDefinition, int priority)
    {
        _logger.LogInformation("Creating new version of rule {RuleId}", id);

        var originalRule = await _ruleRepository.GetByIdAsync(id);
        if (originalRule == null)
        {
            throw new InvalidOperationException($"Original rule with ID {id} not found");
        }

        // Validate the rule definition
        var validationResult = await ValidateRuleDefinitionAsync(ruleDefinition);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors);
            throw new ArgumentException($"Invalid rule definition: {errors}");
        }

        // Create version record for the current state
        await CreateVersionRecord(originalRule, "New version created");

        // Deactivate the original rule
        originalRule.IsActive = false;
        originalRule.UpdatedAt = DateTime.UtcNow;
        await _ruleRepository.UpdateAsync(originalRule);

        // Create new rule as the active version
        var newRule = new Rule
        {
            Name = name,
            Description = description,
            RuleDefinition = ruleDefinition,
            Priority = priority,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createdRule = await _ruleRepository.CreateAsync(newRule);

        // Create version record for the new rule
        await CreateVersionRecord(createdRule, $"New version of rule {id}");

        _logger.LogInformation("Created new version {NewRuleId} of rule {OriginalRuleId}", createdRule.Id, id);
        return createdRule;
    }

    private async Task CreateVersionRecord(Rule rule, string changeReason)
    {
        var latestVersion = await _ruleVersionRepository.GetLatestVersionAsync(rule.Id);
        var nextVersion = latestVersion?.Version + 1 ?? 1;

        var versionRecord = new RuleVersion
        {
            OriginalRuleId = rule.Id,
            Name = rule.Name,
            Description = rule.Description,
            RuleDefinition = rule.RuleDefinition,
            Priority = rule.Priority,
            IsActive = rule.IsActive,
            Version = nextVersion,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System", // TODO: Get from current user context
            ChangeReason = changeReason
        };

        await _ruleVersionRepository.CreateAsync(versionRecord);
        _logger.LogDebug("Created version record {Version} for rule {RuleId}", nextVersion, rule.Id);
    }
}