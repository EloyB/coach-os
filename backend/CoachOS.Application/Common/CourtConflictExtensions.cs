using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Common;

/// <summary>
/// Baan-bezettingscheck, gedeeld door alle services die een lesmoment inplannen of verplaatsen
/// (LessonSerieService, StandaloneLessonService, LessonRescheduleService). Bundelt de repository-
/// aanroep en de Nederlandse foutmelding zodat die niet driemaal apart onderhouden hoeven worden.
/// </summary>
public static class CourtConflictExtensions
{
    /// <summary>
    /// Checkt of <paramref name="courtName"/> al bezet is op het gegeven tijdstip en geeft, bij een
    /// conflict, een gebruiksklare <see cref="Error"/> terug. Geef <paramref name="tennisClubId"/> mee
    /// zodra de les bij een club-gebonden reeks hoort — baannamen zijn vrije tekst per club, dus zonder
    /// deze scoping botst "Baan 2" bij club A onterecht met een gelijknamige baan bij club B. Zonder
    /// club (losse les) blijft de check org-breed.
    /// </summary>
    public static async Task<Error?> CheckCourtConflictAsync(
        this ILessonRepository lessonRepo,
        Guid organizationId, string? courtName, DateOnly date, TimeOnly startTime, TimeOnly endTime,
        Guid? excludeLessonId = null, Guid? tennisClubId = null, CancellationToken ct = default)
    {
        // Geen baan opgegeven → geen bezetting mogelijk.
        if (string.IsNullOrWhiteSpace(courtName))
            return null;

        Lesson? conflict = await lessonRepo.FindCourtConflictAsync(
            organizationId, courtName, date, startTime, endTime, excludeLessonId, tennisClubId, ct);

        if (conflict is null)
            return null;

        string seriesName = conflict.LessonSerie?.Name ?? "onbekende reeks";
        string conflictTime = $"{conflict.StartTime:HH:mm}–{conflict.EndTime:HH:mm}";
        return new Error(ErrorCodes.Conflict,
            $"{courtName.Trim()} is op {conflict.Date:dd/MM/yyyy} van {conflictTime} al bezet door reeks {seriesName}.");
    }
}
