namespace CoachOS.API.Auth;

/// <summary>
/// Named ASP.NET Core authorization policies. Gebruik via
/// <c>.RequireAuthorization(AuthorizationPolicies.SuperAdmin)</c>.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>System-level super admin. Geen org-scope.</summary>
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>
    /// Read-only toegang tot inschrijvingen en planning: rol Admin, of een hoofdtrainer
    /// (Trainer met de <see cref="CoachOsClaims.IsHeadTrainer"/> claim). Enkel op GET-endpoints.
    /// </summary>
    public const string EnrollmentsPlanningRead = "EnrollmentsPlanningRead";
}
