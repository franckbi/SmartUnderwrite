using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartUnderwrite.Api.Models.Application;
using SmartUnderwrite.Api.Services;
using System.Security.Claims;

namespace SmartUnderwrite.Api.Controllers;

[ApiController]
[Route("api/decisions")]
[Authorize]
public class DecisionController : ControllerBase
{
    private readonly IDecisionService _decisionService;
    private readonly ILogger<DecisionController> _logger;

    public DecisionController(IDecisionService decisionService, ILogger<DecisionController> logger)
    {
        _decisionService = decisionService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a decision for a loan application
    /// </summary>
    /// <param name="request">The decision request</param>
    /// <returns>The created decision</returns>
    [HttpPost]
    [Authorize(Policy = "UnderwriterOrAdmin")]
    public async Task<ActionResult<DecisionDto>> CreateDecision([FromBody] DecisionRequest request)
    {
        try
        {
            _logger.LogInformation("Creating decision for application {ApplicationId}", request.ApplicationId);
            
            // Convert DecisionRequest to ManualDecisionRequest
            var manualDecisionRequest = new ManualDecisionRequest
            {
                Outcome = request.Outcome,
                Reasons = request.Reasons,
                Justification = request.Notes
            };

            var decision = await _decisionService.MakeManualDecisionAsync(request.ApplicationId, manualDecisionRequest, User);
            return Ok(decision);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Application not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized access: {Message}", ex.Message);
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid operation: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating decision for application {ApplicationId}", request.ApplicationId);
            return StatusCode(500, new { message = "An error occurred while creating the decision" });
        }
    }

    /// <summary>
    /// Evaluates a loan application using the automated rules engine
    /// </summary>
    /// <param name="applicationId">The ID of the application to evaluate</param>
    /// <returns>The automated decision result</returns>
    [HttpPost("{applicationId}/evaluate")]
    [Authorize(Policy = "UnderwriterOrAdmin")]
    public async Task<ActionResult<DecisionDto>> EvaluateApplication(int applicationId)
    {
        try
        {
            _logger.LogInformation("Evaluating application {ApplicationId}", applicationId);
            var decision = await _decisionService.EvaluateApplicationAsync(applicationId);
            return Ok(decision);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Application not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid operation: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating application {ApplicationId}", applicationId);
            return StatusCode(500, new { message = "An error occurred while evaluating the application" });
        }
    }

    /// <summary>
    /// Makes a manual decision override for a loan application
    /// </summary>
    /// <param name="applicationId">The ID of the application</param>
    /// <param name="request">The manual decision request</param>
    /// <returns>The manual decision result</returns>
    [HttpPost("{applicationId}/manual-decision")]
    [Authorize(Policy = "UnderwriterOrAdmin")]
    public async Task<ActionResult<DecisionDto>> MakeManualDecision(int applicationId, [FromBody] ManualDecisionRequest request)
    {
        try
        {
            _logger.LogInformation("Making manual decision for application {ApplicationId}", applicationId);
            var decision = await _decisionService.MakeManualDecisionAsync(applicationId, request, User);
            return Ok(decision);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Application not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized access: {Message}", ex.Message);
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid operation: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making manual decision for application {ApplicationId}", applicationId);
            return StatusCode(500, new { message = "An error occurred while making the manual decision" });
        }
    }

    /// <summary>
    /// Gets the latest decision for a loan application
    /// </summary>
    /// <param name="applicationId">The ID of the application</param>
    /// <returns>The latest decision or null if none exists</returns>
    [HttpGet("{applicationId}/latest")]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<DecisionDto>> GetLatestDecision(int applicationId)
    {
        try
        {
            _logger.LogDebug("Getting latest decision for application {ApplicationId}", applicationId);
            var decision = await _decisionService.GetLatestDecisionAsync(applicationId, User);
            
            if (decision == null)
            {
                return NotFound(new { message = "No decisions found for this application" });
            }

            return Ok(decision);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized access: {Message}", ex.Message);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest decision for application {ApplicationId}", applicationId);
            return StatusCode(500, new { message = "An error occurred while retrieving the decision" });
        }
    }

    /// <summary>
    /// Gets the decision history for a loan application
    /// </summary>
    /// <param name="applicationId">The ID of the application</param>
    /// <returns>List of all decisions for the application</returns>
    [HttpGet("{applicationId}/history")]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<List<DecisionDto>>> GetDecisionHistory(int applicationId)
    {
        try
        {
            _logger.LogDebug("Getting decision history for application {ApplicationId}", applicationId);
            var decisions = await _decisionService.GetDecisionHistoryAsync(applicationId, User);
            return Ok(decisions);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized access: {Message}", ex.Message);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting decision history for application {ApplicationId}", applicationId);
            return StatusCode(500, new { message = "An error occurred while retrieving the decision history" });
        }
    }

    /// <summary>
    /// Gets a paginated list of decisions
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <returns>Paginated list of decisions</returns>
    [HttpGet]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<PagedResult<DecisionDto>>> GetDecisions(
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 10)
    {
        try
        {
            _logger.LogDebug("Getting decisions with pagination: page {PageNumber}, size {PageSize}", pageNumber, pageSize);
            var decisions = await _decisionService.GetDecisionsAsync(pageNumber, pageSize, User);
            return Ok(decisions);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized access: {Message}", ex.Message);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting decisions list");
            return StatusCode(500, new { message = "An error occurred while retrieving decisions" });
        }
    }

    /// <summary>
    /// Gets a summary of decision statistics
    /// </summary>
    /// <returns>Decision summary statistics</returns>
    [HttpGet("summary")]
    [Authorize(Policy = "AllRoles")]
    public async Task<ActionResult<DecisionSummaryDto>> GetDecisionSummary()
    {
        try
        {
            _logger.LogDebug("Getting decision summary");
            var summary = await _decisionService.GetDecisionSummaryAsync(User);
            return Ok(summary);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized access: {Message}", ex.Message);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting decision summary");
            return StatusCode(500, new { message = "An error occurred while retrieving the decision summary" });
        }
    }
}