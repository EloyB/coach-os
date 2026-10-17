namespace CoachOS.Infrastructure.Identity;

/// <summary>
/// ASP.NET Identity user met CoachOS-specifieke velden.
/// Leeft in Infrastructure (niet Domain) omdat het IdentityUser uitbreidt.
///
/// Organisatie-lidmaatschap en rol zitten op <see cref="Domain.Entities.OrganizationMembership"/>.
/// Een user kan in meerdere organisaties lid zijn met verschillende rollen.
/// </summary>
public class ApplicationUser : Microsoft.AspNetCore.Identity.IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// System-level super admin capability. Een super admin opereert los van organisaties
    /// (Option C in #91): mag GEEN <see cref="Domain.Entities.OrganizationMembership"/> hebben.
    /// Wordt enkel gezet via de bootstrap-service of door een bestaande super admin.
    /// </summary>
    public bool IsSuperAdmin { get; set; }

    /// <summary>Geboortedatum - verplicht voor GDPR bij minderjarigen.</summary>
    public DateOnly? DateOfBirth { get; set; }

    public string? InviteToken { get; set; }
    public DateTime? InviteTokenExpiry { get; set; }

    /// <summary>Hash van de openstaande e-mailverificatiecode; null zodra bevestigd of nooit aangevraagd.</summary>
    public string? EmailVerificationCodeHash { get; set; }
    public DateTime? EmailVerificationCodeExpiry { get; set; }

    /// <summary>Wanneer de laatste code verstuurd werd — begrenst hoe vaak een nieuwe aangevraagd kan worden.</summary>
    public DateTime? EmailVerificationSentAt { get; set; }

    /// <summary>Aantal pogingen op de huidige code.</summary>
    public int EmailVerificationAttempts { get; set; }

    /// <summary>Totaal aantal verstuurde codes voor dit account.</summary>
    public int EmailVerificationSendCount { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
