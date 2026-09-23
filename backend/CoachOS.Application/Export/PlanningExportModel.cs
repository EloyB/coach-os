namespace CoachOS.Application.Export;

/// <summary>
/// Platte, presentatie-onafhankelijke weergave van een lessenreeks-planning,
/// klaar om naar drie Excel-tabbladen geschreven te worden. De
/// <see cref="IPlanningWorkbookBuilder"/> (Infrastructure) zet dit om naar bytes.
/// </summary>
public sealed class PlanningExportModel
{
    public string SeriesName { get; init; } = string.Empty;

    /// <summary>Datum waarop de export gegenereerd is (banner-subtitel).</summary>
    public DateOnly ExportedOn { get; init; }

    /// <summary>
    /// Labels van de custom formuliervelden, in volgorde. Bepaalt de extra
    /// kolommen op het inschrijvingen-tabblad zodat elke rij dezelfde kolommen heeft.
    /// </summary>
    public IReadOnlyList<string> FormFieldLabels { get; init; } = [];

    public IReadOnlyList<EnrollmentRow> Enrollments { get; init; } = [];
    public IReadOnlyList<LessonMomentRow> LessonMoments { get; init; } = [];
    public IReadOnlyList<MomentRosterRow> MomentRosters { get; init; } = [];
}

/// <summary>Tab 1 — één inschrijving + spelerdata + custom formulier-antwoorden.</summary>
public sealed record EnrollmentRow(
    string StudentName,
    string EnrollmentType,
    string? GroupName,
    string StudentEmail,
    string? StudentPhone,
    string Status,
    DateTime EnrolledAt,
    string? Notes,
    IReadOnlyDictionary<string, string> FormResponses);

/// <summary>Tab 2 — één concreet lesmoment (weekslot uitgevouwen naar een datum).</summary>
public sealed record LessonMomentRow(
    DateOnly Date,
    string DayName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? TrainerName,
    string? CourtName,
    int MaxStudents);

/// <summary>
/// Tab — één terugkerend wekelijks lesmoment met de spelers die erop ingedeeld zijn.
/// Compact overzicht "wie zit waar", niet uitgevouwen per datum.
/// </summary>
public sealed record MomentRosterRow(
    string DayName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? TrainerName,
    string? CourtName,
    int MaxStudents,
    IReadOnlyList<RosterPlayer> Players);

/// <summary>Eén speler binnen een <see cref="MomentRosterRow"/>.</summary>
public sealed record RosterPlayer(string Name, string? GroupName, string Status);
