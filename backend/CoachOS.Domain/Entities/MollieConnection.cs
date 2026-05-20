using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>
/// OAuth-koppeling tussen een CoachOS <see cref="Organization"/> en een Mollie-organisatie.
/// 1-1 met <see cref="Organization"/>; bestaan van een rij = "verbonden".
///
/// Tokens zijn versleuteld at rest via <c>IDataProtectionProvider</c>; de plain values
/// verlaten de Application-laag nooit en worden bij elk gebruik on-demand ontsleuteld.
/// </summary>
public class MollieConnection : BaseEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>Identifier van de Mollie-organisatie (bv. <c>org_12345</c>).</summary>
    public string MollieOrganizationId { get; set; } = string.Empty;

    /// <summary>Versleutelde access token; ontsleutel via <c>ITokenProtector</c> voor gebruik.</summary>
    public string AccessTokenEncrypted { get; set; } = string.Empty;

    /// <summary>Versleutelde refresh token; ontsleutel via <c>ITokenProtector</c> voor gebruik.</summary>
    public string RefreshTokenEncrypted { get; set; } = string.Empty;

    /// <summary>UTC vervaltijd van de access token; gebruikt om proactief te refreshen.</summary>
    public DateTime AccessTokenExpiresAt { get; set; }

    /// <summary>Cached display-naam van de Mollie-organisatie voor UI ("Verbonden met ...").</summary>
    public string? MollieOrganizationName { get; set; }

    /// <summary>UTC moment waarop de koppeling initieel werd gemaakt.</summary>
    public DateTime ConnectedAt { get; set; }

    public Organization Organization { get; set; } = null!;
}
