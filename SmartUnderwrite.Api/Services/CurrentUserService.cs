using SmartUnderwrite.Api.Constants;
using System.Security.Claims;

namespace SmartUnderwrite.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CurrentUserService> _logger;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, ILogger<CurrentUserService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public ClaimsPrincipal GetUser()
    {
        return _httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();
    }

    public int? GetUserId()
    {
        var userIdClaim = GetUser().FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }

    public int? GetAffiliateId()
    {
        var affiliateIdClaim = GetUser().FindFirst("affiliateId");
        if (affiliateIdClaim != null && int.TryParse(affiliateIdClaim.Value, out var affiliateId))
        {
            return affiliateId;
        }
        return null;
    }

    public List<string> GetRoles()
    {
        return GetUser().FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
    }

    public bool IsInRole(string role)
    {
        return GetUser().IsInRole(role);
    }

    public bool CanAccessAffiliate(int affiliateId)
    {
        var roles = GetRoles();
        
        // Admin and Underwriter can access all affiliates
        if (roles.Contains(Roles.Admin) || roles.Contains(Roles.Underwriter))
        {
            return true;
        }

        // Affiliate users can only access their own affiliate
        if (roles.Contains(Roles.Affiliate))
        {
            var userAffiliateId = GetAffiliateId();
            return userAffiliateId.HasValue && userAffiliateId.Value == affiliateId;
        }

        _logger.LogWarning("User with roles [{Roles}] attempted to access affiliate {AffiliateId}", 
            string.Join(", ", roles), affiliateId);
        
        return false;
    }
}