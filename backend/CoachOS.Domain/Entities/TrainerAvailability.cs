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

    public Guid TennisClubId { get; set; }

    /// <summary>0 = maandag ... 6 = zondag (zelfde conventie als WeeklyTemplateEntry).</summary>
    public int DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    /// <summary>Soft delete - verwijderen zet IsActive op false.</summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public TennisClub TennisClub { get; set; } = null!;
}
