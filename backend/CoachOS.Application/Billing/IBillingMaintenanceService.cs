namespace CoachOS.Application.Billing;

/// <summary>Achtergrondwerk voor abonnementen: facturen aanmaken en dagelijkse opvolging.</summary>
public interface IBillingMaintenanceService
{
    /// <summary>Factuur-outbox: maakt openstaande facturen aan (met backoff). Geeft het aantal nieuwe facturen terug.</summary>
    Task<int> ProcessPendingInvoicesAsync(CancellationToken ct = default);

    /// <summary>
    /// Eén keer per dag: verlengingsfactuur 30 dagen voor het einde van een jaarabonnement per
    /// overschrijving, lopende periode opschuiven, en afgelopen deals beëindigen.
    /// </summary>
    Task RunDailyAsync(DateOnly today, CancellationToken ct = default);
}
