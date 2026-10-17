using CoachOS.Application.Auth.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Auth;

public interface IAuthService
{
    /// <summary>
    /// Maakt organisatie, admin, lidmaatschap en een gratis proef aan, legt de aanvaarding van
    /// de voorwaarden vast en geeft meteen een ingelogde sessie terug. Geen betaalgegevens nodig.
    /// </summary>
    Task<Result<AuthResponseDto>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Bevestigt het e-mailadres van de gebruiker met de code uit de verificatiemail.</summary>
    Task<Result> VerifyEmailAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stuurt een nieuwe verificatiecode (maximaal één per minuut). Met
    /// <paramref name="onlyIfExpired"/> gebeurt dat enkel als er geen geldige code meer openstaat —
    /// zo krijgt wie later terugkeert een verse code, zonder een nog bruikbare ongeldig te maken.
    /// De waarde is true als er effectief een mail vertrokken is.
    /// </summary>
    Task<Result<bool>> ResendEmailVerificationAsync(
        Guid userId,
        bool onlyIfExpired = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// True als het e-mailadres van de gebruiker bevestigd is. Acties die mails naar derden
    /// versturen (trainers, leerlingen) vereisen dit.
    /// </summary>
    Task<bool> IsEmailVerifiedAsync(
        Guid userId,
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
