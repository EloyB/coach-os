namespace CoachOS.Application.LessonSerie.DTOs;

public record UpdateLessonRequest
{
    public Guid? TrainerId { get; init; }
    public string? Date { get; init; }
    public string? StartTime { get; init; }
    public string? EndTime { get; init; }
    public string? CourtName { get; init; }
    public int? MaxStudents { get; init; }
    public string? Notes { get; init; }
    public bool? IsCancelled { get; init; }
    public string? CancellationReason { get; init; }

    /// <summary>
    /// Reikwijdte van de wijziging: <c>"slot"</c> past de recurring attributen (tijd, trainer,
    /// baan, capaciteit) toe op het hele weekslot — de <see cref="WeeklyTemplateEntry"/> én alle
    /// niet-geannuleerde lessen ervan — zodat de planning meegaat. <c>"lesson"</c> (of null) raakt
    /// enkel deze les. Datum en annulering blijven altijd per les.
    /// </summary>
    public string? ApplyTo { get; init; }
}
