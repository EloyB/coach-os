using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Eenmalige, kortlevende token die per e-mail naar een student gestuurd wordt
/// om in te loggen zonder wachtwoord. Identiteit = e-mailadres (geen ApplicationUser).
/// </summary>
public class MagicLinkToken : BaseEntity
{
    public string Email { get; set; } = string.Empty;

    /// <summary>SHA256 hash van de token (hex, 64 chars). De ruwe token gaat enkel per e-mail.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}
