using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartUnderwrite.Api.Constants;
using SmartUnderwrite.Api.Models.Rules;
using SmartUnderwrite.Core.RulesEngine.Interfaces;

namespace SmartUnderwrite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Underwriter}")]
public class RulesController : ControllerBase
{
    private readonly IRuleService _ruleService;
    private readonly ILogger<RulesController> _logger;

    public RulesController(IRuleService ruleService, ILogger<RulesController> logger)
    {
        _ruleService = ruleService ?? throw new ArgumentNullException(nameof(ruleService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all rules (active and inactive)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RuleDto>>> GetAllRules()
    {
        try
        {
            var rules = await _ruleService.GetAllRulesAsync();
            var ruleDtos = rules.Select(r => new RuleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                RuleDefinition = r.RuleDefinition,
                Priority = r.Priority,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            });

            return Ok(ruleDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all rules");
            return StatusCode(500, new { message = "An error occurred while retrieving rules" });
        }
    }

    /// <summary>
    /// Gets all active rules ordered by priority
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<RuleDto>>> GetActiveRules()
    {
        try
        {
            var rules = await _ruleService.GetActiveRulesAsync();
            var ruleDtos = rules.Select(r => new RuleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                RuleDefinition = r.RuleDefinition,
                Priority = r.Priority,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            });

            return Ok(ruleDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active rules");
            return StatusCode(500, new { message = "An error occurred while retrieving active rules" });
        }
    }

    /// <summary>
    /// Gets a specific rule by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<RuleDto>> GetRule(int id)
    {
        try
        {
            var rule = await _ruleService.GetRuleByIdAsync(id);
            if (rule == null)
            {
                return NotFound(new { message = "Rule not found" });
            }

            var ruleDto = new RuleDto
            {
                Id = rule.Id,
                Name = rule.Name,
                Description = rule.Description,
                RuleDefinition = rule.RuleDefinition,
                Priority = rule.Priority,
                IsActive = rule.IsActive,
                CreatedAt = rule.CreatedAt,
                UpdatedAt = rule.UpdatedAt
            };

            return Ok(ruleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rule {RuleId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the rule" });
        }
    }

    /// <summary>
    /// Creates a new rule
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<RuleDto>> CreateRule([FromBody] CreateRuleRequest request)
    {
        try
        {
            var rule = await _ruleService.CreateRuleAsync(
                request.Name,
                request.Description,
                request.RuleDefinition,
                request.Priority);

            var ruleDto = new RuleDto
            {
                Id = rule.Id,
                Name = rule.Name,
                Description = rule.Description,
                RuleDefinition = rule.RuleDefinition,
                Priority = rule.Priority,
                IsActive = rule.IsActive,
                CreatedAt = rule.CreatedAt,
                UpdatedAt = rule.UpdatedAt
            };

            _logger.LogInformation("Created rule {RuleId}: {RuleName}", rule.Id, rule.Name);
            return CreatedAtAction(nameof(GetRule), new { id = rule.Id }, ruleDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid rule definition provided");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating rule");
            return StatusCode(500, new { message = "An error occurred while creating the rule" });
        }
    }

    /// <summary>
    /// Updates an existing rule
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<RuleDto>> UpdateRule(int id, [FromBody] UpdateRuleRequest request)
    {
        try
        {
            var rule = await _ruleService.UpdateRuleAsync(
                id,
                request.Name,
                request.Description,
                request.RuleDefinition,
                request.Priority);

            var ruleDto = new RuleDto
            {
                Id = rule.Id,
                Name = rule.Name,
                Description = rule.Description,
                RuleDefinition = rule.RuleDefinition,
                Priority = rule.Priority,
                IsActive = rule.IsActive,
                CreatedAt = rule.CreatedAt,
                UpdatedAt = rule.UpdatedAt
            };

            _logger.LogInformation("Updated rule {RuleId}: {RuleName}", rule.Id, rule.Name);
            return Ok(ruleDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid rule definition provided for rule {RuleId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Rule {RuleId} not found for update", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rule {RuleId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the rule" });
        }
    }

    /// <summary>
    /// Activates a rule
    /// </summary>
    [HttpPost("{id}/activate")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<RuleDto>> ActivateRule(int id)
    {
        try
        {
            var rule = await _ruleService.ActivateRuleAsync(id);

            var ruleDto = new RuleDto
            {
                Id = rule.Id,
                Name = rule.Name,
                Description = rule.Description,
                RuleDefinition = rule.RuleDefinition,
                Priority = rule.Priority,
                IsActive = rule.IsActive,
                CreatedAt = rule.CreatedAt,
                UpdatedAt = rule.UpdatedAt
            };

            _logger.LogInformation("Activated rule {RuleId}: {RuleName}", rule.Id, rule.Name);
            return Ok(ruleDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Rule {RuleId} not found for activation", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating rule {RuleId}", id);
            return StatusCode(500, new { message = "An error occurred while activating the rule" });
        }
    }

    /// <summary>
    /// Deactivates a rule
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<RuleDto>> DeactivateRule(int id)
    {
        try
        {
            var rule = await _ruleService.DeactivateRuleAsync(id);

            var ruleDto = new RuleDto
            {
                Id = rule.Id,
                Name = rule.Name,
                Description = rule.Description,
                RuleDefinition = rule.RuleDefinition,
                Priority = rule.Priority,
                IsActive = rule.IsActive,
                CreatedAt = rule.CreatedAt,
                UpdatedAt = rule.UpdatedAt
            };

            _logger.LogInformation("Deactivated rule {RuleId}: {RuleName}", rule.Id, rule.Name);
            return Ok(ruleDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Rule {RuleId} not found for deactivation", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating rule {RuleId}", id);
            return StatusCode(500, new { message = "An error occurred while deactivating the rule" });
        }
    }

    /// <summary>
    /// Deletes a rule permanently
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteRule(int id)
    {
        try
        {
            var deleted = await _ruleService.DeleteRuleAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = "Rule not found" });
            }

            _logger.LogInformation("Deleted rule {RuleId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting rule {RuleId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the rule" });
        }
    }

    /// <summary>
    /// Validates a rule definition without saving
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<RuleValidationResponse>> ValidateRule([FromBody] ValidateRuleRequest request)
    {
        try
        {
            var validationResult = await _ruleService.ValidateRuleDefinitionAsync(request.RuleDefinition);

            var response = new RuleValidationResponse
            {
                IsValid = validationResult.IsValid,
                Errors = validationResult.Errors.ToList(),
                Warnings = validationResult.Warnings.ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating rule definition");
            return StatusCode(500, new { message = "An error occurred while validating the rule definition" });
        }
    }

    /// <summary>
    /// Gets the version history of a rule
    /// </summary>
    [HttpGet("{id}/history")]
    public async Task<ActionResult<IEnumerable<RuleVersionDto>>> GetRuleHistory(int id)
    {
        try
        {
            var versions = await _ruleService.GetRuleHistoryAsync(id);
            var versionDtos = versions.Select(v => new RuleVersionDto
            {
                Id = v.Id,
                OriginalRuleId = v.OriginalRuleId,
                Name = v.Name,
                Description = v.Description,
                RuleDefinition = v.RuleDefinition,
                Priority = v.Priority,
                IsActive = v.IsActive,
                Version = v.Version,
                CreatedAt = v.CreatedAt,
                CreatedBy = v.CreatedBy,
                ChangeReason = v.ChangeReason
            });

            return Ok(versionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rule history for rule {RuleId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving rule history" });
        }
    }

    /// <summary>
    /// Creates a new version of an existing rule
    /// </summary>
    [HttpPost("{id}/version")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<RuleDto>> CreateRuleVersion(int id, [FromBody] UpdateRuleRequest request)
    {
        try
        {
            var rule = await _ruleService.CreateRuleVersionAsync(
                id,
                request.Name,
                request.Description,
                request.RuleDefinition,
                request.Priority);

            var ruleDto = new RuleDto
            {
                Id = rule.Id,
                Name = rule.Name,
                Description = rule.Description,
                RuleDefinition = rule.RuleDefinition,
                Priority = rule.Priority,
                IsActive = rule.IsActive,
                CreatedAt = rule.CreatedAt,
                UpdatedAt = rule.UpdatedAt
            };

            _logger.LogInformation("Created new version {NewRuleId} of rule {OriginalRuleId}", rule.Id, id);
            return CreatedAtAction(nameof(GetRule), new { id = rule.Id }, ruleDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid rule definition provided for creating version of rule {RuleId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Rule {RuleId} not found for creating version", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating version of rule {RuleId}", id);
            return StatusCode(500, new { message = "An error occurred while creating the rule version" });
        }
    }
}