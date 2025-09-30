using Microsoft.AspNetCore.Authorization;
using SmartUnderwrite.Api.Constants;

namespace SmartUnderwrite.Api.Attributes;

public class AuthorizeRolesAttribute : AuthorizeAttribute
{
    public AuthorizeRolesAttribute(params string[] roles)
    {
        Roles = string.Join(",", roles);
    }
}

public class AdminOnlyAttribute : AuthorizeAttribute
{
    public AdminOnlyAttribute()
    {
        Policy = Policies.AdminOnly;
    }
}

public class UnderwriterOrAdminAttribute : AuthorizeAttribute
{
    public UnderwriterOrAdminAttribute()
    {
        Policy = Policies.UnderwriterOrAdmin;
    }
}

public class AffiliateAccessAttribute : AuthorizeAttribute
{
    public AffiliateAccessAttribute()
    {
        Policy = Policies.AffiliateAccess;
    }
}