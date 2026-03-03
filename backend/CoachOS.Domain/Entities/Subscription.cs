using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Abonnement van een organisatie op CoachOS (Starter/Pro/Business/Enterprise).
/// </summary>
public class Subscription : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public SubscriptionPlan Plan { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? MollieSubscriptionId { get; set; }
    public string? MollieCustomerId { get; set; }

    /// <summary>Maandelijks bedrag in EUR.</summary>
    public decimal MonthlyPrice { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
}
