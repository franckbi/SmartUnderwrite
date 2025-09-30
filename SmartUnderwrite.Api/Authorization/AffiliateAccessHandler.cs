using Microsoft.AspNetCore.Authorization;
using SmartUnderwrite.Api.Constants;
using System.Security.Claims;

namespace SmartUnderwrite.Api.Authorization;

public class AffiliateAccessHandler : AuthorizationHandler<AffiliateAccessRequirement>
{
    private readonly ILogger<AffiliateAccessHandler> _logger;

    public AffiliateAccessHandler(ILogger<AffiliateAccessHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AffiliateAccessRequirement requirement)
    {
        var user = context.User;
        
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("User is not authenticated");
            return Task.CompletedTask;
        }

        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        
        // Admin users have access to everything if allowed
        if (requirement.AllowAdminAccess && roles.Contains(Roles.Admin))
        {
            _logger.LogDebug("Admin user granted access");
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Underwriter users have access to all data if allowed
        if (requirement.AllowUnderwriterAccess && roles.Contains(Roles.Underwriter))
        {
            _logger.LogDebug("Underwriter user granted access");
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Affiliate users can only access their own data
        if (roles.Contains(Roles.Affiliate))
        {
            var affiliateIdClaim = user.FindFirst("affiliateId");
            if (affiliateIdClaim != null && int.TryParse(affiliateIdClaim.Value, out var affiliateId))
            {
                // The specific affiliate ID validation will be handled in the controller/service layer
                // Here we just verify the user has an affiliate ID
                _logger.LogDebug("Affiliate user {AffiliateId} granted access", affiliateId);
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
            else
            {
                _logger.LogWarning("Affiliate user does not have a valid affiliate ID claim");
            }
        }

        _logger.LogWarning("User with roles [{Roles}] denied access", string.Join(", ", roles));
        return Task.CompletedTask;
    }
}