namespace SmartUnderwrite.Api.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Underwriter = "Underwriter";
    public const string Affiliate = "Affiliate";
}

public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string UnderwriterOrAdmin = "UnderwriterOrAdmin";
    public const string AffiliateAccess = "AffiliateAccess";
    public const string AllRoles = "AllRoles";
}