using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Abonnement van een organisatie op CoachOS. Elke organisatie heeft er precies één:
/// bestaande organisaties staan op <see cref="SubscriptionStatus.Legacy"/>, nieuwe starten
/// met een gratis proef (<see cref="StartTrial"/>).
/// </summary>
public class Subscription : BaseEntity
{
    public const int TrialDays = 30;

    public Guid OrganizationId { get; set; }

    /// <summary>Gekozen plan; null zolang er nog niet betaald wordt (proef of legacy).</summary>
    public SubscriptionPlan? Plan { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Legacy;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    /// <summary>Laatste dag van de proef (inclusief). Enkel gezet bij <see cref="SubscriptionStatus.Trialing"/>.</summary>
    public DateOnly? TrialEndsAt { get; set; }

    /// <summary>
    /// Laatst verstuurde proef-herinnering (20, 27 of 30 = dag in de proef). Voorkomt dat
    /// dezelfde herinnering twee keer vertrekt.
    /// </summary>
    public int? LastTrialReminderDay { get; set; }

    public bool IsActive { get; set; } = true;
    public string? MollieSubscriptionId { get; set; }
    public string? MollieCustomerId { get; set; }

    /// <summary>Maandelijks bedrag in EUR.</summary>
    public decimal MonthlyPrice { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;

    /// <summary>Nieuwe proef van <see cref="TrialDays"/> dagen, zonder plan of betaalgegevens.</summary>
    public static Subscription StartTrial(Guid organizationId, DateOnly today) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        Status = SubscriptionStatus.Trialing,
        StartDate = today,
        TrialEndsAt = today.AddDays(TrialDays),
        IsActive = true
    };
}
