namespace CoachOS.API.Auth;

/// <summary>
/// Custom JWT claim names voor CoachOS.
/// </summary>
public static class CoachOsClaims
{
    /// <summary>"true" als de user system-level super admin is. Aanwezig op super-admin tokens, afwezig op org-tokens.</summary>
    public const string IsSuperAdmin = "isSuperAdmin";

    /// <summary>Organization id voor org-scoped tokens. Afwezig op super-admin tokens.</summary>
    public const string OrganizationId = "organizationId";

    /// <summary>"true" als de trainer hoofdtrainer is (read-only inschrijvingen + planning).</summary>
    public const string IsHeadTrainer = "isHeadTrainer";
}
