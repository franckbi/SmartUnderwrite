using Microsoft.AspNetCore.Authorization;

namespace SmartUnderwrite.Api.Authorization;

public class AffiliateAccessRequirement : IAuthorizationRequirement
{
    public AffiliateAccessRequirement(bool allowAdminAccess = true, bool allowUnderwriterAccess = true)
    {
        AllowAdminAccess = allowAdminAccess;
        AllowUnderwriterAccess = allowUnderwriterAccess;
    }

    public bool AllowAdminAccess { get; }
    public bool AllowUnderwriterAccess { get; }
}