using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Aanwezigheid van een trainer op een kampdag, met een eigen tijdvenster
/// (kan korter zijn dan de kampuren: halve dag, een paar uur).
/// </summary>
public class CampDayTrainer : BaseEntity
{
    public Guid CampDayId { get; set; }
    public Guid OrganizationId { get; set; }

    /// <summary>Plain Guid zonder FK (ApplicationUser zit in Infrastructure/Identity).</summary>
    public Guid TrainerId { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    // Navigation
    public CampDay CampDay { get; set; } = null!;
}
