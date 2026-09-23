using CoachOS.Domain.Common;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using LessonSerieEntity = CoachOS.Domain.Entities.LessonSerie;

namespace CoachOS.Application.Export;

/// <summary>
/// Verzamelt de planningsdata van één lessenreeks en bouwt daaruit een Excel-export.
/// De terugkerende weekslots worden uitgevouwen naar concrete datums binnen de
/// reeksperiode; <see cref="ScheduleAssignment"/>s (op weekslot-niveau) worden per
/// datum aan de spelers gekoppeld. Geweigerde toewijzingen worden weggelaten.
/// </summary>
public class PlanningExportService(
    ILessonSerieRepository seriesRepo,
    IEnrollmentRepository enrollmentRepo,
    IEnrollmentGroupRepository groupRepo,
    IScheduleAssignmentRepository assignmentRepo,
    IUserLookupService userLookup,
    IPlanningWorkbookBuilder workbookBuilder,
    TimeProvider timeProvider) : IPlanningExportService
{
    private const string XlsxContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public async Task<Result<ExportFileDto>> ExportSeriePlanningAsync(
        Guid serieId, Guid organizationId, CancellationToken ct = default)
    {
        LessonSerieEntity? series = await seriesRepo.GetByIdAsync(serieId, organizationId, ct);
        if (series is null)
            return Result<ExportFileDto>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        List<Enrollment> enrollments = await enrollmentRepo.GetBySeriesAsync(serieId, organizationId, ct);
        List<EnrollmentGroup> groups = await groupRepo.GetBySeriesAsync(serieId, organizationId, ct);
        List<ScheduleAssignment> assignments = await assignmentRepo.GetBySeriesAsync(serieId, organizationId, ct);

        Dictionary<Guid, string> trainerNames = await ResolveTrainerNamesAsync(series, ct);

        DateOnly today = timeProvider.GetBrusselsToday();

        Dictionary<Guid, Enrollment> enrollmentsById = enrollments.ToDictionary(e => e.Id);
        Dictionary<Guid, EnrollmentGroup> groupsById = groups.ToDictionary(g => g.Id);
        Dictionary<Guid, List<AssignedPlayer>> playersBySlot =
            BuildPlayersBySlot(assignments, enrollmentsById, groupsById);

        PlanningExportModel model = new()
        {
            SeriesName = series.Name,
            ExportedOn = today,
            FormFieldLabels = CollectFormFieldLabels(enrollments),
            Enrollments = BuildEnrollmentRows(enrollments, groupsById),
            LessonMoments = BuildLessonMomentRows(series, trainerNames),
            MomentRosters = BuildMomentRosters(series, playersBySlot, trainerNames),
        };

        byte[] content = workbookBuilder.Build(model);
        string fileName = BuildFileName(series.Name, today);
        return Result<ExportFileDto>.Ok(new ExportFileDto(content, fileName, XlsxContentType));
    }

    private async Task<Dictionary<Guid, string>> ResolveTrainerNamesAsync(
        LessonSerieEntity series, CancellationToken ct)
    {
        List<Guid> trainerIds = series.WeeklyTemplate
            .Where(s => s.TrainerId.HasValue)
            .Select(s => s.TrainerId!.Value)
            .Distinct()
            .ToList();

        return trainerIds.Count > 0
            ? await userLookup.GetUserNamesByIdsAsync(trainerIds, ct)
            : new Dictionary<Guid, string>();
    }

    private static IReadOnlyList<string> CollectFormFieldLabels(List<Enrollment> enrollments)
        => enrollments
            .SelectMany(e => e.FormResponses)
            .Where(r => r.FormField is not null)
            .Select(r => new { r.FormField.Order, r.FormField.Label })
            .DistinctBy(x => x.Label)
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Label)
            .Select(x => x.Label)
            .ToList();

    private static IReadOnlyList<EnrollmentRow> BuildEnrollmentRows(
        List<Enrollment> enrollments, Dictionary<Guid, EnrollmentGroup> groupsById)
        => enrollments
            .Select(e =>
            {
                EnrollmentGroup? group = e.EnrollmentGroupId.HasValue
                    && groupsById.TryGetValue(e.EnrollmentGroupId.Value, out EnrollmentGroup? g) ? g : null;
                bool isLeader = group is not null && group.LeaderEnrollmentId == e.Id;
                string type = group is null ? "Individueel" : isLeader ? "Groepsleider" : "Groepslid";
                return (Enrollment: e, Group: group, IsLeader: isLeader, Type: type);
            })
            // Individuen eerst (lege groepsnaam), daarna groepen geclusterd met de leider bovenaan.
            .OrderBy(x => x.Group?.Name ?? string.Empty)
            .ThenByDescending(x => x.IsLeader)
            .ThenBy(x => x.Enrollment.StudentName)
            .Select(x => new EnrollmentRow(
                x.Enrollment.StudentName,
                x.Type,
                x.Group?.Name,
                x.Enrollment.ContactEmail,
                x.Enrollment.StudentPhone,
                EnrollmentStatusLabel(x.Enrollment.Status),
                x.Enrollment.EnrolledAt,
                x.Enrollment.Notes,
                x.Enrollment.FormResponses
                    .Where(r => r.FormField is not null)
                    .GroupBy(r => r.FormField.Label)
                    .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(r => r.Value)))))
            .ToList();

    private IReadOnlyList<LessonMomentRow> BuildLessonMomentRows(
        LessonSerieEntity series, Dictionary<Guid, string> trainerNames)
    {
        List<LessonMomentRow> rows = [];

        foreach (WeeklyTemplateEntry slot in series.WeeklyTemplate)
        {
            string? trainerName = slot.TrainerId.HasValue
                && trainerNames.TryGetValue(slot.TrainerId.Value, out string? name)
                    ? name
                    : null;

            foreach (DateOnly date in ExpandDates(series.StartDate, series.EndDate, slot.DayOfWeek))
            {
                rows.Add(new LessonMomentRow(
                    date, DutchDay(date.DayOfWeek), slot.StartTime, slot.EndTime,
                    trainerName, slot.CourtName, slot.MaxStudents));
            }
        }

        return rows
            .OrderBy(r => r.Date)
            .ThenBy(r => r.StartTime)
            .ToList();
    }

    /// <summary>Per weekslot de spelers die er (niet-geweigerd) op ingedeeld zijn.</summary>
    private static Dictionary<Guid, List<AssignedPlayer>> BuildPlayersBySlot(
        List<ScheduleAssignment> assignments,
        Dictionary<Guid, Enrollment> enrollmentsById,
        Dictionary<Guid, EnrollmentGroup> groupsById)
    {
        Dictionary<Guid, List<AssignedPlayer>> playersBySlot = [];
        foreach (ScheduleAssignment a in assignments)
        {
            if (a.Status == ScheduleAssignmentStatus.Declined)
                continue;

            string statusLabel = AssignmentStatusLabel(a.Status);
            List<AssignedPlayer> players = playersBySlot.TryGetValue(a.WeeklyTemplateEntryId, out List<AssignedPlayer>? existing)
                ? existing
                : playersBySlot[a.WeeklyTemplateEntryId] = [];

            if (a.EnrollmentGroupId.HasValue && groupsById.TryGetValue(a.EnrollmentGroupId.Value, out EnrollmentGroup? group))
            {
                foreach (Enrollment member in group.Members)
                    players.Add(new AssignedPlayer(member.StudentName, group.Name, statusLabel));
            }
            else if (a.EnrollmentId.HasValue && enrollmentsById.TryGetValue(a.EnrollmentId.Value, out Enrollment? enrollment))
            {
                players.Add(new AssignedPlayer(enrollment.StudentName, null, statusLabel));
            }
        }
        return playersBySlot;
    }

    private static readonly string[] AppDayNames =
        ["Maandag", "Dinsdag", "Woensdag", "Donderdag", "Vrijdag", "Zaterdag", "Zondag"];

    private static IReadOnlyList<MomentRosterRow> BuildMomentRosters(
        LessonSerieEntity series,
        Dictionary<Guid, List<AssignedPlayer>> playersBySlot,
        Dictionary<Guid, string> trainerNames)
    {
        List<MomentRosterRow> rows = [];

        foreach (WeeklyTemplateEntry slot in series.WeeklyTemplate)
        {
            if (!playersBySlot.TryGetValue(slot.Id, out List<AssignedPlayer>? players) || players.Count == 0)
                continue;

            string? trainerName = slot.TrainerId.HasValue
                && trainerNames.TryGetValue(slot.TrainerId.Value, out string? name)
                    ? name
                    : null;

            List<RosterPlayer> roster = players
                .OrderBy(p => p.GroupName ?? string.Empty)
                .ThenBy(p => p.Name)
                .Select(p => new RosterPlayer(p.Name, p.GroupName, p.Status))
                .ToList();

            int dayIndex = Math.Clamp(slot.DayOfWeek, 0, 6);
            rows.Add(new MomentRosterRow(
                AppDayNames[dayIndex], slot.StartTime, slot.EndTime,
                trainerName, slot.CourtName, slot.MaxStudents, roster));
        }

        return rows
            .OrderBy(r => Array.IndexOf(AppDayNames, r.DayName))
            .ThenBy(r => r.StartTime)
            .ToList();
    }

    private static string BuildFileName(string seriesName, DateOnly today)
    {
        string safe = new(seriesName
            .Select(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' ? c : '-')
            .ToArray());
        safe = safe.Trim();
        if (string.IsNullOrWhiteSpace(safe))
            safe = "lessenreeks";

        return $"{safe}-planning-{today:yyyyMMdd}.xlsx";
    }

    private static IEnumerable<DateOnly> ExpandDates(DateOnly start, DateOnly end, int dayOfWeek)
    {
        // WeeklyTemplateEntry.DayOfWeek gebruikt de app-conventie (0=maandag ... 6=zondag),
        // niet System.DayOfWeek (0=zondag) — vandaar de conversie i.p.v. een rechtstreekse vergelijking.
        for (DateOnly d = start; d <= end; d = d.AddDays(1))
            if (((int)d.DayOfWeek + 6) % 7 == dayOfWeek)
                yield return d;
    }

    private static string DutchDay(DayOfWeek d) => d switch
    {
        DayOfWeek.Monday => "Maandag",
        DayOfWeek.Tuesday => "Dinsdag",
        DayOfWeek.Wednesday => "Woensdag",
        DayOfWeek.Thursday => "Donderdag",
        DayOfWeek.Friday => "Vrijdag",
        DayOfWeek.Saturday => "Zaterdag",
        DayOfWeek.Sunday => "Zondag",
        _ => d.ToString(),
    };

    private static string EnrollmentStatusLabel(EnrollmentStatus s) => s switch
    {
        EnrollmentStatus.Pending => "In afwachting",
        EnrollmentStatus.Confirmed => "Bevestigd",
        EnrollmentStatus.Cancelled => "Geannuleerd",
        EnrollmentStatus.Waitlisted => "Wachtlijst",
        EnrollmentStatus.PendingPayment => "Wacht op betaling",
        _ => s.ToString(),
    };

    private static string AssignmentStatusLabel(ScheduleAssignmentStatus s) => s switch
    {
        ScheduleAssignmentStatus.Proposed => "Voorstel",
        ScheduleAssignmentStatus.Confirmed => "Bevestigd",
        ScheduleAssignmentStatus.AwaitingConfirmation => "Wacht op bevestiging",
        ScheduleAssignmentStatus.Declined => "Geweigerd",
        _ => s.ToString(),
    };

    private readonly record struct AssignedPlayer(string Name, string? GroupName, string Status);
}
