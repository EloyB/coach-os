namespace CoachOS.Application.LessonReschedule.DTOs;

/// <summary>
/// Resultaat van een replan-actie: id van de nieuwe les + ontvangers die een mail kregen.
/// </summary>
public record RescheduleLessonResultDto(
    Guid NewLessonId,
    int NotifiedCount);
