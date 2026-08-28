namespace CoachOS.Application.LessonSerie.DTOs;

/// <summary>
/// Past een bestaand weekslot (<c>WeeklyTemplateEntry</c>) aan vanuit de planning-view.
/// De wijziging geldt voor het slot én al z'n niet-geannuleerde lessen, zodat de planning meegaat.
/// De weekdag ligt vast (verplaatsen naar een andere dag is geen onderdeel hiervan).
/// </summary>
public record UpdateWeekSlotRequest
{
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public Guid? TrainerId { get; init; }
    public string? CourtName { get; init; }
    public int MaxStudents { get; init; }
}
