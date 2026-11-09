using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>Eén dag van een kamp, met een optioneel voormiddag- en namiddagdeel (minstens één).</summary>
public class CampDay : BaseEntity
{
    public Guid CampId { get; set; }
    public Guid OrganizationId { get; set; }
    public DateOnly Date { get; set; }

    public TimeOnly? MorningStart { get; set; }
    public TimeOnly? MorningEnd { get; set; }
    public TimeOnly? AfternoonStart { get; set; }
    public TimeOnly? AfternoonEnd { get; set; }

    /// <summary>Vroegste start van de bestaande dagdelen (kampuren zoals trainers ze zien).</summary>
    public TimeOnly FirstStart => MorningStart ?? AfternoonStart ?? default;

    /// <summary>Laatste einde van de bestaande dagdelen.</summary>
    public TimeOnly LastEnd => AfternoonEnd ?? MorningEnd ?? default;

    public bool HasPart(CampDayPart part)
        => part == CampDayPart.Morning ? MorningStart.HasValue : AfternoonStart.HasValue;

    // Navigation
    public Camp Camp { get; set; } = null!;
    public ICollection<CampDayTrainer> TrainerAssignments { get; set; } = new List<CampDayTrainer>();
}
