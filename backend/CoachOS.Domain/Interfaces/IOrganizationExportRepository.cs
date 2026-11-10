using CoachOS.Domain.Models;

namespace CoachOS.Domain.Interfaces;

/// <summary>Leest alle gegevens van één organisatie voor de clubexport (AsNoTracking).</summary>
public interface IOrganizationExportRepository
{
    /// <summary>
    /// True als de gebruiker een actieve beheerder van de organisatie is met een actief,
    /// bevestigd account (geen superadmin).
    /// </summary>
    Task<bool> IsActiveConfirmedAdminAsync(Guid organizationId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Alle tabellen van de organisatie met veilige kolommen: geen wachtwoorden, tokens of
    /// geheimen, en nooit rijen van andere organisaties.
    /// </summary>
    Task<IReadOnlyList<ExportTable>> GetTablesAsync(Guid organizationId, CancellationToken ct = default);
}
