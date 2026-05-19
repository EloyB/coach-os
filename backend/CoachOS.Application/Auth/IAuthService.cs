using CoachOS.Application.Auth.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Auth;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(
        string organizationName,
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<Result<AuthResponseDto>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>Wissel naar een andere organisatie waar de user lid van is. Retourneert nieuwe JWT.</summary>
    Task<Result<AuthResponseDto>> SwitchOrganizationAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>Lijst alle (actieve) memberships van de user — voor de FE org-switcher.</summary>
    Task<Result<List<OrganizationMembershipDto>>> GetMembershipsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Start de wachtwoord-reset flow: genereert een token en stuurt een e-mail.
    /// Geeft altijd Ok terug — ook als het e-mailadres onbekend is (anti-enumeration).
    /// </summary>
    Task<Result> ForgotPasswordAsync(
        string email,
        string resetBaseUrl,
        CancellationToken cancellationToken = default);

    /// <summary>Valideert het reset-token en stelt een nieuw wachtwoord in.</summary>
    Task<Result> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepteert een invite-token: zet wachtwoord, activeert user + pending membership,
    /// en geeft een ingelogde sessie terug. Generiek voor zowel trainer- als admin-invites
    /// (de membership-rol bepaalt waar de user naar landt).
    /// </summary>
    Task<Result<AuthResponseDto>> AcceptInviteAsync(
        string token,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Valideert een invite-token zonder het te consumeren. Gebruikt door de FE
    /// om bij page-load te checken of de link nog geldig is — voorkomt dat een
    /// reeds geaccepteerde invite alsnog het formulier toont.
    /// </summary>
    Task<Result<InviteValidationDto>> ValidateInviteTokenAsync(
        string token,
        CancellationToken cancellationToken = default);
}
