using CoachOS.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace CoachOS.Infrastructure.Identity;

/// <summary>
/// ASP.NET Identity user met CoachOS-specifieke velden.
/// Leeft in Infrastructure (niet Domain) omdat het IdentityUser uitbreidt.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public Guid OrganizationId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Geboortedatum - verplicht voor GDPR bij minderjarigen.</summary>
    public DateOnly? DateOfBirth { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
