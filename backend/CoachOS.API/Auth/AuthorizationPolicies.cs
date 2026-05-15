namespace CoachOS.API.Auth;

/// <summary>
/// Named ASP.NET Core authorization policies. Gebruik via
/// <c>.RequireAuthorization(AuthorizationPolicies.SuperAdmin)</c>.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>System-level super admin. Geen org-scope.</summary>
    public const string SuperAdmin = "SuperAdmin";
}
