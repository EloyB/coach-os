using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Vaste beschikbaarheid van een trainer: club x weekdag x tijdvak.
/// Door de admin vastgelegd. Gebruikt om de trainerkeuze bij reeks-setup
/// te ondersteunen en dubbelboekingen over clubs heen te signaleren.
/// </summary>
public class TrainerAvailability : BaseEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Plain Guid zonder FK - ApplicationUser zit in Infrastructure (Identity),
    /// zelfde patroon als LessonSlotBase.TrainerId.
    /// </summary>
    public Guid TrainerId { get; set; }

    /// <summary>
    /// Optioneel. Null betekent "beschikbaar bij eender welke club" - dan hoeft
    /// de admin niet per club een aparte beschikbaarheid aan te maken.
    /// </summary>
    public Guid? TennisClubId { get; set; }

    /// <summary>0 = maandag ... 6 = zondag (zelfde conventie als WeeklyTemplateEntry).</summary>
    public int DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    /// <summary>Soft delete - verwijderen zet IsActive op false.</summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Organization Organization { get; set; } = null!;

    /// <summary>Null wanneer de beschikbaarheid voor eender welke club geldt.</summary>
    public TennisClub? TennisClub { get; set; }
}
