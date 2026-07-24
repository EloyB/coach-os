namespace CoachOS.Application.LessonSerie.DTOs;

/// <summary>
/// Voegt een wekelijks terugkerend lesmoment (weekslot) toe aan een bestaande lessenreeks.
/// De backend expandeert dit naar concrete lesmomenten voor elke matchende weekdag vanaf vandaag
/// tot de einddatum van de reeks.
/// </summary>
public record AddWeeklyTemplateEntryRequest
{
    /// <summary>0 = maandag … 6 = zondag.</summary>
    public int DayOfWeek { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public Guid? TrainerId { get; init; }
    public string? CourtName { get; init; }
    public int MaxStudents { get; init; }
    public int? Level { get; init; }
}
