namespace CoachOS.Application.Abstractions;

/// <summary>Bezorgstatus van een verwijdermail in de outbox.</summary>
public enum EmailDeliveryStatus
{
    /// <summary>Geen mail gevonden (nooit ingepland, of al opgeruimd).</summary>
    Missing = 1,

    /// <summary>Ingepland of wordt verstuurd.</summary>
    Pending = 2,

    /// <summary>Verstuurd.</summary>
    Sent = 3,

    /// <summary>Definitief mislukt.</summary>
    Failed = 4
}

/// <summary>
/// Leest of een verwijdermail echt vertrokken is. De organisatieretentie verwijdert pas als de
/// laatste mail verstuurd is. Cross-org; enkel voor de systeemjob.
/// </summary>
public interface IOrganizationDeletionNoticeStatus
{
    /// <summary>
    /// Status van de verwijdermails voor <paramref name="organizationId"/> die op of na
    /// <paramref name="queuedSinceUtc"/> ingepland werden (de huidige stap van de planning). Op de
    /// inhoud kan niet gezocht worden: de outbox wist die na verzending. Bij meerdere pogingen telt
    /// de beste status.
    /// </summary>
    Task<EmailDeliveryStatus> GetAsync(Guid organizationId, DateTime queuedSinceUtc, CancellationToken ct = default);
}
