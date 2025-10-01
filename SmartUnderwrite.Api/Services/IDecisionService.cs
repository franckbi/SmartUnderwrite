using SmartUnderwrite.Api.Models.Application;
using SmartUnderwrite.Core.Entities;
using System.Security.Claims;

namespace SmartUnderwrite.Api.Services;

public interface IDecisionService
{
    /// <summary>
    /// Evaluates a loan application using the rules engine and creates an automated decision
    /// </summary>
    /// <param name="applicationId">The ID of the application to evaluate</param>
    /// <returns>The created decision</returns>
    Task<DecisionDto> EvaluateApplicationAsync(int applicationId);

    /// <summary>
    /// Creates a manual decision override for an application
    /// </summary>
    /// <param name="applicationId">The ID of the application</param>
    /// <param name="request">The manual decision request</param>
    /// <param name="user">The user making the decision</param>
    /// <returns>The created decision</returns>
    Task<DecisionDto> MakeManualDecisionAsync(int applicationId, ManualDecisionRequest request, ClaimsPrincipal user);

    /// <summary>
    /// Gets the latest decision for an application
    /// </summary>
    /// <param name="applicationId">The ID of the application</param>
    /// <param name="user">The requesting user</param>
    /// <returns>The latest decision or null if none exists</returns>
    Task<DecisionDto?> GetLatestDecisionAsync(int applicationId, ClaimsPrincipal user);

    /// <summary>
    /// Gets all decisions for an application
    /// </summary>
    /// <param name="applicationId">The ID of the application</param>
    /// <param name="user">The requesting user</param>
    /// <returns>List of decisions ordered by creation date</returns>
    Task<List<DecisionDto>> GetDecisionHistoryAsync(int applicationId, ClaimsPrincipal user);

    /// <summary>
    /// Gets a paginated list of decisions
    /// </summary>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="user">The requesting user</param>
    /// <returns>Paginated list of decisions</returns>
    Task<PagedResult<DecisionDto>> GetDecisionsAsync(int pageNumber, int pageSize, ClaimsPrincipal user);

    /// <summary>
    /// Gets a summary of decision statistics
    /// </summary>
    /// <param name="user">The requesting user</param>
    /// <returns>Decision summary statistics</returns>
    Task<DecisionSummaryDto> GetDecisionSummaryAsync(ClaimsPrincipal user);
}