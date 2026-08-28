using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Enkelvoudig lesmoment, al dan niet onderdeel van een LessonSerie.
/// </summary>
public class Lesson : LessonSlotBase
{
    public Guid OrganizationId { get; set; }

    /// <summary>Null voor losse lessen (niet onderdeel van een reeks).</summary>
    public Guid? LessonSerieId { get; set; }

    /// <summary>
    /// De club waar deze les doorgaat. Voor reeks-lessen wordt de club altijd via
    /// <see cref="LessonSerie"/>.TennisClubId bepaald (dit veld blijft dan null — geen dubbele
    /// opslag). Voor losse lessen (LessonSerieId == null) is dit de bron van waarheid; null
    /// betekent een legacy losse les van vóór deze kolom, waarvan de club onbekend is.
    /// </summary>
    public Guid? TennisClubId { get; set; }

    public DateOnly Date { get; set; }
    public LessonLevel? Level { get; set; }
    public string? Notes { get; set; }
    public bool IsCancelled { get; set; }
    public string? CancellationReason { get; set; }

    /// <summary>Verwijst naar de nieuwe les die deze (geannuleerde) les vervangt.</summary>
    public Guid? RescheduledToLessonId { get; set; }

    /// <summary>
    /// Het weekslot (<see cref="WeeklyTemplateEntry"/>) waaruit deze les gegenereerd is.
    /// Null voor losse lessen die niet uit een weekslot komen. Maakt "pas het hele weekslot aan"
    /// deterministisch: alle lessen van één slot delen deze id, en de planning leest de template.
    /// </summary>
    public Guid? WeeklyTemplateEntryId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public LessonSerie? LessonSerie { get; set; }
    public TennisClub? TennisClub { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public Lesson? RescheduledToLesson { get; set; }
    public WeeklyTemplateEntry? WeeklyTemplateEntry { get; set; }
}
