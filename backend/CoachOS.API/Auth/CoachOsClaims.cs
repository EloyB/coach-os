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

    /// <summary>Club-id waarvan de trainer hoofdtrainer is. 0..n claims van dit type per token.</summary>
    public const string HeadTrainerClub = "headTrainerClub";
}
