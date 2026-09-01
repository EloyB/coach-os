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

        PlanningExportModel model = new()
        {
            SeriesName = series.Name,
            ExportedOn = today,
            FormFieldLabels = CollectFormFieldLabels(enrollments),
            Enrollments = BuildEnrollmentRows(enrollments),
            LessonMoments = BuildLessonMomentRows(series, trainerNames),
            ScheduledLessons = BuildScheduledRows(series, assignments, enrollments, groups),
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

    private static IReadOnlyList<EnrollmentRow> BuildEnrollmentRows(List<Enrollment> enrollments)
        => enrollments
            .OrderBy(e => e.StudentName)
            .Select(e => new EnrollmentRow(
                e.StudentName,
                e.ContactEmail,
                e.StudentPhone,
                EnrollmentStatusLabel(e.Status),
                e.EnrolledAt,
                e.Notes,
                e.FormResponses
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

    private static IReadOnlyList<ScheduledRow> BuildScheduledRows(
        LessonSerieEntity series,
        List<ScheduleAssignment> assignments,
        List<Enrollment> enrollments,
        List<EnrollmentGroup> groups)
    {
        Dictionary<Guid, Enrollment> enrollmentsById = enrollments.ToDictionary(e => e.Id);
        Dictionary<Guid, EnrollmentGroup> groupsById = groups.ToDictionary(g => g.Id);

        // Per weekslot de spelers die er (niet-geweigerd) op ingedeeld zijn.
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
                    players.Add(new AssignedPlayer(member.StudentName, member.ContactEmail, group.Name, statusLabel));
            }
            else if (a.EnrollmentId.HasValue && enrollmentsById.TryGetValue(a.EnrollmentId.Value, out Enrollment? enrollment))
            {
                players.Add(new AssignedPlayer(enrollment.StudentName, enrollment.ContactEmail, null, statusLabel));
            }
        }

        List<ScheduledRow> rows = [];
        foreach (WeeklyTemplateEntry slot in series.WeeklyTemplate)
        {
            if (!playersBySlot.TryGetValue(slot.Id, out List<AssignedPlayer>? players))
                continue;

            foreach (DateOnly date in ExpandDates(series.StartDate, series.EndDate, slot.DayOfWeek))
            {
                foreach (AssignedPlayer p in players)
                {
                    rows.Add(new ScheduledRow(
                        date, slot.StartTime, slot.EndTime,
                        p.Name, p.Email, p.GroupName, p.Status));
                }
            }
        }

        return rows
            .OrderBy(r => r.Date)
            .ThenBy(r => r.StartTime)
            .ThenBy(r => r.StudentName)
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
        // WeeklyTemplateEntry.DayOfWeek (0 = zondag) lijnt 1-op-1 op System.DayOfWeek.
        for (DateOnly d = start; d <= end; d = d.AddDays(1))
            if ((int)d.DayOfWeek == dayOfWeek)
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

    private readonly record struct AssignedPlayer(string Name, string Email, string? GroupName, string Status);
}
