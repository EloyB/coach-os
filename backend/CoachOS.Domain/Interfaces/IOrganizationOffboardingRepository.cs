using CoachOS.Domain.Models;

namespace CoachOS.Domain.Interfaces;

/// <summary>
/// Verwijdering van een volledige organisatie (offboarding door de superadmin). Cross-org:
/// negeert de tenant query filter en scopet expliciet op <c>organizationId</c>.
/// </summary>
public interface IOrganizationOffboardingRepository
{
    /// <summary>Aantallen per gegevenstype, inclusief accounts die enkel bij deze organisatie horen.</summary>
    Task<OrganizationDataCounts> CountDataAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Vergrendelt deze ene organisatie tot het einde van de lopende transactie: advisory lock
    /// per organisatie plus rijlocks op de organisatie en haar abonnement. Andere organisaties
    /// blijven ongemoeid. Vereist een actieve transactie.
    /// </summary>
    Task LockOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Verwijdert alle gegevens van de organisatie en de accounts die enkel bij deze organisatie
    /// hoorden. Accounts met een lidmaatschap of trainerrol bij een andere organisatie blijven.
    /// Abonnementsbetalingen (facturen) worden nooit verwijderd: bestaan ze, dan faalt de
    /// verwijdering op de foreign key. Geeft het aantal verwijderde accounts terug.
    /// </summary>
    Task<int> DeleteOrganizationAsync(Guid organizationId, CancellationToken ct = default);
}
