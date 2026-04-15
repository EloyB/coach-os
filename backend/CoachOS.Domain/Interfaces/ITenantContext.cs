namespace CoachOS.Domain.Interfaces;

/// <summary>
/// Ambient tenant-context voor de huidige request/scope. Gevuld door middleware
/// na authenticatie. Wordt door <c>ApplicationDbContext</c> gebruikt voor global
/// query filters zodat elke query automatisch binnen de actieve organisatie blijft.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Actieve organisatie voor deze scope. <c>Guid.Empty</c> als er geen
    /// tenant is gezet (anonieme of publieke request).
    /// </summary>
    Guid OrganizationId { get; }

    /// <summary>Actieve user (JWT sub claim). <c>Guid.Empty</c> indien anoniem.</summary>
    Guid UserId { get; }

    /// <summary>True zodra een tenant gezet is.</summary>
    bool IsAuthenticated { get; }
}
