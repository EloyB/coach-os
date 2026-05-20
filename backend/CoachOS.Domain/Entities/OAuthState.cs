using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Eénmalige CSRF-state voor de Mollie OAuth-flow. Wordt aangemaakt bij
/// "Connect to Mollie" (admin met JWT) en verbruikt door de anonieme callback
/// endpoint om de bijhorende <see cref="OrganizationId"/> te herstellen.
///
/// Rijen worden verwijderd na succesvolle verbruik of na verloop van
/// <see cref="ExpiresAt"/> via een achtergrond-cleanup taak.
/// </summary>
public class OAuthState : BaseEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>Cryptografisch random state-token; unique.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>UTC moment waarop deze state ongeldig wordt (typisch +15 minuten).</summary>
    public DateTime ExpiresAt { get; set; }
}
