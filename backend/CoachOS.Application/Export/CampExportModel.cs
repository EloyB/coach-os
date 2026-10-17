namespace CoachOS.Application.Export;

/// <summary>
/// Platte, presentatie-onafhankelijke weergave van een kamp, klaar om naar twee
/// Excel-tabbladen geschreven te worden (inschrijvingen + kampdagen).
/// </summary>
public sealed class CampExportModel
{
    public string CampName { get; init; } = string.Empty;
    public DateOnly ExportedOn { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }

    /// <summary>Labels van de custom formuliervelden, in volgorde (extra kolommen).</summary>
    public IReadOnlyList<string> FormFieldLabels { get; init; } = [];

    public IReadOnlyList<CampEnrollmentRow> Enrollments { get; init; } = [];
    public IReadOnlyList<CampDayRow> Days { get; init; } = [];
}

/// <summary>Tab 1 — één kamp-inschrijving + betaling + custom formulier-antwoorden.</summary>
public sealed record CampEnrollmentRow(
    string ParticipantName,
    string EnrollmentType,
    string? GroupName,
    string Email,
    string? Phone,
    string Status,
    string? PaymentStatus,
    string? PaymentMethod,
    DateTime EnrolledAt,
    string? Notes,
    IReadOnlyDictionary<string, string> FormResponses);

/// <summary>Tab 2 — één kampdag met de kampuren en de aanwezige trainers.</summary>
public sealed record CampDayRow(
    DateOnly Date,
    string DayName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    IReadOnlyList<CampDayTrainerRow> Trainers);

public sealed record CampDayTrainerRow(string Name, TimeOnly StartTime, TimeOnly EndTime);
