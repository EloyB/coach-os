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

    /// <summary>Geboortedatum - verplicht voor GDPR bij minderjarigen.</summary>
    public DateOnly? DateOfBirth { get; set; }

    public string? InviteToken { get; set; }
    public DateTime? InviteTokenExpiry { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
