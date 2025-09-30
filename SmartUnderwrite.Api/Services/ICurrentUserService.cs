using System.Security.Claims;

namespace SmartUnderwrite.Api.Services;

public interface ICurrentUserService
{
    int? GetUserId();
    int? GetAffiliateId();
    List<string> GetRoles();
    bool IsInRole(string role);
    bool CanAccessAffiliate(int affiliateId);
    ClaimsPrincipal GetUser();
}