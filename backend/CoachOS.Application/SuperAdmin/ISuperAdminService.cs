using CoachOS.Application.SuperAdmin.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.SuperAdmin;

/// <summary>
/// System-level beheer van admins en organisaties. Alle methodes zijn cross-org
/// en bypassen de normale OrganizationId-filtering — daarom enkel bereikbaar
/// via endpoints achter de SuperAdmin authorization policy.
/// </summary>
public interface ISuperAdminService
{
    /// <summary>Maakt een nieuwe organisatie + admin-user (zonder wachtwoord) en stuurt invite-mail.</summary>
    Task<Result<Guid>> CreateAdminWithOrganizationAsync(
        CreateAdminRequest request,
        string inviteBaseUrl,
        CancellationToken ct = default);

    /// <summary>Lijst alle admins (over alle orgs) — voor het super-admin overzicht.</summary>
    Task<Result<List<AdminListItemDto>>> ListAdminsAsync(CancellationToken ct = default);

    /// <summary>Deactiveert een admin-user (IsActive=false). Werkt cross-org. Kan geen super admin disablen.</summary>
    Task<Result> DisableAdminAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Activeert een eerder gedeactiveerde admin-user.</summary>
    Task<Result> EnableAdminAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Verstuurt de invite-mail opnieuw als de admin z'n wachtwoord nog niet heeft ingesteld.</summary>
    Task<Result> ResendAdminInviteAsync(Guid userId, string inviteBaseUrl, CancellationToken ct = default);

    /// <summary>Lijst alle organisaties met early-bird status en admin-count.</summary>
    Task<Result<List<OrganizationListItemDto>>> ListOrganizationsAsync(CancellationToken ct = default);

    /// <summary>Zet de Early-Bird flag op een organisatie. Lifetime discount-bron.</summary>
    Task<Result> SetOrganizationEarlyBirdAsync(Guid organizationId, bool isEarlyBird, CancellationToken ct = default);
}
