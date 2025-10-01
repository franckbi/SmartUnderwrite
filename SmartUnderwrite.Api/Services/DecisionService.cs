using Microsoft.EntityFrameworkCore;
using SmartUnderwrite.Api.Models.Application;
using SmartUnderwrite.Api.Services;
using SmartUnderwrite.Core.Entities;
using SmartUnderwrite.Core.Enums;
using SmartUnderwrite.Core.RulesEngine.Interfaces;
using SmartUnderwrite.Infrastructure.Data;
using System.Security.Claims;

namespace SmartUnderwrite.Api.Services;

public class DecisionService : IDecisionService
{
    private readonly SmartUnderwriteDbContext _context;
    private readonly IRulesEngine _rulesEngine;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DecisionService> _logger;

    public DecisionService(
        SmartUnderwriteDbContext context,
        IRulesEngine rulesEngine,
        ICurrentUserService currentUserService,
        ILogger<DecisionService> logger)
    {
        _context = context;
        _rulesEngine = rulesEngine;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<DecisionDto> EvaluateApplicationAsync(int applicationId)
    {
        _logger.LogInformation("Starting automated evaluation for application {ApplicationId}", applicationId);

        // Get the application with related data
        var application = await _context.LoanApplications
            .Include(la => la.Applicant)
            .Include(la => la.Affiliate)
            .FirstOrDefaultAsync(la => la.Id == applicationId);

        if (application == null)
        {
            throw new ArgumentException($"Application with ID {applicationId} not found", nameof(applicationId));
        }

        // Check if application is in a state that allows evaluation
        if (application.Status != ApplicationStatus.Submitted && application.Status != ApplicationStatus.InReview)
        {
            throw new InvalidOperationException($"Application {applicationId} cannot be evaluated in status {application.Status}");
        }

        try
        {
            // Evaluate using rules engine
            var evaluationResult = await _rulesEngine.EvaluateAsync(application, application.Applicant);

            // Create decision entity
            var decision = new Decision
            {
                LoanApplicationId = applicationId,
                Outcome = evaluationResult.Outcome,
                Score = evaluationResult.Score,
                Reasons = evaluationResult.Reasons.ToArray(),
                DecidedByUserId = null, // Automated decision
                DecidedAt = DateTime.UtcNow
            };

            // Add decision to context
            _context.Decisions.Add(decision);

            // Update application status based on decision outcome
            var newStatus = evaluationResult.Outcome switch
            {
                DecisionOutcome.Approve => ApplicationStatus.Approved,
                DecisionOutcome.Reject => ApplicationStatus.Rejected,
                DecisionOutcome.ManualReview => ApplicationStatus.InReview,
                _ => throw new InvalidOperationException($"Unknown decision outcome: {evaluationResult.Outcome}")
            };

            application.Status = newStatus;
            application.UpdatedAt = DateTime.UtcNow;

            // Save changes
            await _context.SaveChangesAsync();

            _logger.LogInformation("Automated evaluation completed for application {ApplicationId}. Outcome: {Outcome}, Score: {Score}",
                applicationId, evaluationResult.Outcome, evaluationResult.Score);

            return MapToDto(decision);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during automated evaluation of application {ApplicationId}", applicationId);
            throw;
        }
    }

    public async Task<DecisionDto> MakeManualDecisionAsync(int applicationId, ManualDecisionRequest request, ClaimsPrincipal user)
    {
        var userId = _currentUserService.GetUserId();
        var userName = user.Identity?.Name;

        _logger.LogInformation("Starting manual decision for application {ApplicationId} by user {UserId}", applicationId, userId);

        // Get the application
        var application = await _context.LoanApplications
            .FirstOrDefaultAsync(la => la.Id == applicationId);

        if (application == null)
        {
            throw new ArgumentException($"Application with ID {applicationId} not found", nameof(applicationId));
        }

        // Check if user has permission to make decisions on this application
        if (!await CanUserMakeDecisionAsync(applicationId, user))
        {
            throw new UnauthorizedAccessException("User does not have permission to make decisions on this application");
        }

        // Check if application allows manual decisions
        if (application.Status == ApplicationStatus.Approved || application.Status == ApplicationStatus.Rejected)
        {
            throw new InvalidOperationException($"Application {applicationId} is already in final status {application.Status}");
        }

        try
        {
            // Create manual decision entity
            var decision = new Decision
            {
                LoanApplicationId = applicationId,
                Outcome = request.Outcome,
                Score = 0, // Manual decisions don't have calculated scores
                Reasons = request.Reasons,
                DecidedByUserId = userId,
                DecidedAt = DateTime.UtcNow
            };

            // Add decision to context
            _context.Decisions.Add(decision);

            // Update application status based on decision outcome
            var newStatus = request.Outcome switch
            {
                DecisionOutcome.Approve => ApplicationStatus.Approved,
                DecisionOutcome.Reject => ApplicationStatus.Rejected,
                DecisionOutcome.ManualReview => ApplicationStatus.InReview,
                _ => throw new InvalidOperationException($"Unknown decision outcome: {request.Outcome}")
            };

            application.Status = newStatus;
            application.UpdatedAt = DateTime.UtcNow;

            // Save changes
            await _context.SaveChangesAsync();

            _logger.LogInformation("Manual decision completed for application {ApplicationId} by user {UserId}. Outcome: {Outcome}",
                applicationId, userId, request.Outcome);

            // Load the decision with user information for the response
            var savedDecision = await _context.Decisions
                .Include(d => d.DecidedByUser)
                .FirstAsync(d => d.Id == decision.Id);

            return MapToDto(savedDecision);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual decision for application {ApplicationId} by user {UserId}", applicationId, userId);
            throw;
        }
    }

    public async Task<DecisionDto?> GetLatestDecisionAsync(int applicationId, ClaimsPrincipal user)
    {
        // Check if user has permission to view this application
        if (!await CanUserViewApplicationAsync(applicationId, user))
        {
            throw new UnauthorizedAccessException("User does not have permission to view this application");
        }

        var decision = await _context.Decisions
            .Include(d => d.DecidedByUser)
            .Where(d => d.LoanApplicationId == applicationId)
            .OrderByDescending(d => d.DecidedAt)
            .FirstOrDefaultAsync();

        return decision != null ? MapToDto(decision) : null;
    }

    public async Task<List<DecisionDto>> GetDecisionHistoryAsync(int applicationId, ClaimsPrincipal user)
    {
        // Check if user has permission to view this application
        if (!await CanUserViewApplicationAsync(applicationId, user))
        {
            throw new UnauthorizedAccessException("User does not have permission to view this application");
        }

        var decisions = await _context.Decisions
            .Include(d => d.DecidedByUser)
            .Where(d => d.LoanApplicationId == applicationId)
            .OrderByDescending(d => d.DecidedAt)
            .ToListAsync();

        return decisions.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<DecisionDto>> GetDecisionsAsync(int pageNumber, int pageSize, ClaimsPrincipal user)
    {
        _logger.LogDebug("Getting paginated decisions: page {PageNumber}, size {PageSize}", pageNumber, pageSize);

        var userRoles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var query = _context.Decisions
            .Include(d => d.LoanApplication)
            .ThenInclude(la => la.Affiliate)
            .Include(d => d.DecidedByUser)
            .AsQueryable();

        // Apply role-based filtering
        if (userRoles.Contains("Affiliate"))
        {
            var userId = _currentUserService.GetUserId();
            var userAffiliate = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.AffiliateId)
                .FirstOrDefaultAsync();

            if (userAffiliate.HasValue)
            {
                query = query.Where(d => d.LoanApplication.AffiliateId == userAffiliate.Value);
            }
            else
            {
                // Affiliate user without affiliate assignment - return empty result
                return new PagedResult<DecisionDto>
                {
                    Items = new List<DecisionDto>(),
                    TotalCount = 0,
                    Page = pageNumber,
                    PageSize = pageSize
                };
            }
        }
        // Admins and underwriters can see all decisions

        var totalCount = await query.CountAsync();
        var decisions = await query
            .OrderByDescending(d => d.DecidedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<DecisionDto>
        {
            Items = decisions.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

    private async Task<bool> CanUserViewApplicationAsync(int applicationId, ClaimsPrincipal user)
    {
        var userRoles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        
        // Admins and underwriters can view all applications
        if (userRoles.Contains("Admin") || userRoles.Contains("Underwriter"))
        {
            return true;
        }

        // Affiliates can only view their own applications
        if (userRoles.Contains("Affiliate"))
        {
            var userId = _currentUserService.GetUserId();
            var userAffiliate = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.AffiliateId)
                .FirstOrDefaultAsync();

            if (userAffiliate.HasValue)
            {
                var applicationAffiliate = await _context.LoanApplications
                    .Where(la => la.Id == applicationId)
                    .Select(la => la.AffiliateId)
                    .FirstOrDefaultAsync();

                return applicationAffiliate == userAffiliate.Value;
            }
        }

        return false;
    }

    private Task<bool> CanUserMakeDecisionAsync(int applicationId, ClaimsPrincipal user)
    {
        var userRoles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        
        // Only admins and underwriters can make manual decisions
        return Task.FromResult(userRoles.Contains("Admin") || userRoles.Contains("Underwriter"));
    }

    public async Task<DecisionSummaryDto> GetDecisionSummaryAsync(ClaimsPrincipal user)
    {
        _logger.LogDebug("Getting decision summary");

        var userRoles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var query = _context.Decisions
            .Include(d => d.LoanApplication)
            .AsQueryable();

        // Apply role-based filtering
        if (userRoles.Contains("Affiliate"))
        {
            var userId = _currentUserService.GetUserId();
            var userAffiliate = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.AffiliateId)
                .FirstOrDefaultAsync();

            if (userAffiliate.HasValue)
            {
                query = query.Where(d => d.LoanApplication.AffiliateId == userAffiliate.Value);
            }
            else
            {
                // Affiliate user without affiliate assignment - return empty summary
                return new DecisionSummaryDto
                {
                    TotalDecisions = 0,
                    ApprovedCount = 0,
                    RejectedCount = 0,
                    ManualReviewCount = 0,
                    AverageScore = 0,
                    ManualDecisionCount = 0,
                    AutomatedDecisionCount = 0
                };
            }
        }
        // Admins and underwriters can see all decisions

        var decisions = await query.ToListAsync();

        var totalDecisions = decisions.Count;
        var approvedCount = decisions.Count(d => d.Outcome == DecisionOutcome.Approve);
        var rejectedCount = decisions.Count(d => d.Outcome == DecisionOutcome.Reject);
        var manualReviewCount = decisions.Count(d => d.Outcome == DecisionOutcome.ManualReview);
        var averageScore = decisions.Any() ? decisions.Average(d => d.Score) : 0;
        var manualDecisionCount = decisions.Count(d => d.DecidedByUserId != null);
        var automatedDecisionCount = decisions.Count(d => d.DecidedByUserId == null);

        return new DecisionSummaryDto
        {
            TotalDecisions = totalDecisions,
            ApprovedCount = approvedCount,
            RejectedCount = rejectedCount,
            ManualReviewCount = manualReviewCount,
            AverageScore = averageScore,
            ManualDecisionCount = manualDecisionCount,
            AutomatedDecisionCount = automatedDecisionCount
        };
    }

    private static DecisionDto MapToDto(Decision decision)
    {
        return new DecisionDto
        {
            Id = decision.Id,
            LoanApplicationId = decision.LoanApplicationId,
            Outcome = decision.Outcome,
            Score = decision.Score,
            Reasons = decision.Reasons,
            DecidedByUserId = decision.DecidedByUserId,
            DecidedByUserName = decision.DecidedByUser != null 
                ? $"{decision.DecidedByUser.FirstName} {decision.DecidedByUser.LastName}".Trim()
                : null,
            DecidedAt = decision.DecidedAt
        };
    }
}