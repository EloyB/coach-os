using System.Globalization;
using CoachOS.Application.Abstractions;
using CoachOS.Domain.Common;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Export;

/// <summary>
/// Verzamelt alle data van één kamp (inschrijvingen, betalingen, formulier-antwoorden,
/// kampdagen + trainers) en bouwt daaruit een Excel-export.
/// </summary>
public class CampExportService(
    ICampRepository campRepo,
    ICampEnrollmentRepository enrollmentRepo,
    IPaymentRepository paymentRepo,
    IUserLookupService userLookup,
    ICampWorkbookBuilder workbookBuilder,
    TimeProvider timeProvider) : ICampExportService
{
    private const string XlsxContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public async Task<Result<ExportFileDto>> ExportCampAsync(
        Guid campId, Guid organizationId, CancellationToken ct = default)
    {
        Camp? camp = await campRepo.GetByIdWithDetailsAsync(campId, organizationId, ct);
        if (camp is null)
            return Result<ExportFileDto>.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        List<CampEnrollment> enrollments =
            await enrollmentRepo.GetByCampWithResponsesAsync(campId, organizationId, ct);

        Dictionary<Guid, (PaymentMethod? Method, PaymentStatus Status)> paymentInfo =
            await paymentRepo.GetLatestMethodAndStatusByCampEnrollmentIdsAsync(
                enrollments.Select(e => e.Id), ct);

        List<Guid> trainerIds = camp.Days
            .SelectMany(d => d.TrainerAssignments)
            .Select(t => t.TrainerId)
            .Distinct()
            .ToList();
        Dictionary<Guid, string> trainerNames = trainerIds.Count > 0
            ? await userLookup.GetUserNamesByIdsAsync(trainerIds, ct)
            : new Dictionary<Guid, string>();

        DateOnly today = timeProvider.GetBrusselsToday();

        CampExportModel model = new()
        {
            CampName = camp.Name,
            ExportedOn = today,
            StartDate = camp.StartDate,
            EndDate = camp.EndDate,
            FormFieldLabels = CollectFormFieldLabels(camp, enrollments),
            Enrollments = BuildEnrollmentRows(enrollments, paymentInfo, camp.Days.ToDictionary(d => d.Id)),
            Days = BuildDayRows(camp, enrollments, trainerNames),
        };

        byte[] content = workbookBuilder.Build(model);
        string fileName = ExportLabels.FileName(camp.Name, "kamp", "kamp", today);
        return Result<ExportFileDto>.Ok(new ExportFileDto(content, fileName, XlsxContentType));
    }

    /// <summary>
    /// Formuliervelden van het kamp (op volgorde), aangevuld met labels die enkel nog
    /// in antwoorden voorkomen (veld intussen verwijderd) zodat er geen data wegvalt.
    /// </summary>
    private static IReadOnlyList<string> CollectFormFieldLabels(Camp camp, List<CampEnrollment> enrollments)
    {
        List<string> labels = (camp.EnrollmentForm?.Fields ?? [])
            .OrderBy(f => f.Order)
            .Select(f => f.Label)
            .ToList();

        IEnumerable<string> extra = enrollments
            .SelectMany(e => e.FormResponses)
            .Where(r => r.CampFormField is not null)
            .Select(r => r.CampFormField.Label);

        foreach (string label in extra)
            if (!labels.Contains(label))
                labels.Add(label);

        return labels;
    }

    private static IReadOnlyList<CampEnrollmentRow> BuildEnrollmentRows(
        List<CampEnrollment> enrollments,
        Dictionary<Guid, (PaymentMethod? Method, PaymentStatus Status)> paymentInfo,
        Dictionary<Guid, CampDay> daysById)
        => enrollments
            .Select(e =>
            {
                CampEnrollmentGroup? group = e.Group;
                bool isLeader = group is not null && group.LeaderEnrollmentId == e.Id;
                string type = group is null ? "Individueel" : isLeader ? "Groepsleider" : "Groepslid";
                return (Enrollment: e, Group: group, IsLeader: isLeader, Type: type);
            })
            // Individuen eerst (lege groepsnaam), daarna groepen geclusterd met de leider bovenaan.
            .OrderBy(x => x.Group?.Name ?? string.Empty)
            .ThenByDescending(x => x.IsLeader)
            .ThenBy(x => x.Enrollment.ParticipantName)
            .Select(x =>
            {
                bool hasPayment = paymentInfo.TryGetValue(x.Enrollment.Id,
                    out (PaymentMethod? Method, PaymentStatus Status) info);
                return new CampEnrollmentRow(
                    x.Enrollment.ParticipantName,
                    x.Type,
                    x.Group?.Name,
                    x.Enrollment.ParticipantEmail,
                    x.Enrollment.ParticipantPhone,
                    ExportLabels.EnrollmentStatus(x.Enrollment.Status),
                    hasPayment ? ExportLabels.PaymentStatus(info.Status) : null,
                    hasPayment && info.Method.HasValue ? ExportLabels.PaymentMethod(info.Method.Value) : null,
                    x.Enrollment.EnrolledAt,
                    x.Enrollment.Notes,
                    x.Enrollment.FormResponses
                        .Where(r => r.CampFormField is not null)
                        .GroupBy(r => r.CampFormField.Label)
                        .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(r => r.Value))),
                    DaysSummary(x.Enrollment, daysById),
                    x.Enrollment.TotalAmount);
            })
            .ToList();

    /// <summary>"06/07 hele dag; 07/07 namiddag", op datum gesorteerd.</summary>
    private static string DaysSummary(CampEnrollment enrollment, Dictionary<Guid, CampDay> daysById)
        => string.Join("; ", enrollment.Days
            .Where(d => daysById.ContainsKey(d.CampDayId))
            .OrderBy(d => daysById[d.CampDayId].Date)
            .Select(d =>
            {
                string part = d.Morning && d.Afternoon ? "hele dag" : d.Morning ? "voormiddag" : "namiddag";
                return $"{daysById[d.CampDayId].Date.ToString("dd/MM", CultureInfo.InvariantCulture)} {part}";
            }));

    private static IReadOnlyList<CampDayRow> BuildDayRows(
        Camp camp, List<CampEnrollment> enrollments, Dictionary<Guid, string> trainerNames)
    {
        List<CampEnrollmentDay> active = enrollments
            .Where(e => e.Status != EnrollmentStatus.Cancelled)
            .SelectMany(e => e.Days)
            .ToList();

        return camp.Days
            .OrderBy(d => d.Date)
            .ThenBy(d => d.FirstStart)
            .Select(d => new CampDayRow(
                d.Date,
                ExportLabels.DutchDay(d.Date.DayOfWeek),
                d.MorningStart, d.MorningEnd, d.AfternoonStart, d.AfternoonEnd,
                active.Count(x => x.CampDayId == d.Id && x.Morning),
                active.Count(x => x.CampDayId == d.Id && x.Afternoon),
                d.TrainerAssignments
                    .Select(t => new CampDayTrainerRow(
                        trainerNames.GetValueOrDefault(t.TrainerId, "Onbekend"), t.StartTime, t.EndTime))
                    .OrderBy(t => t.Name)
                    .ThenBy(t => t.StartTime)
                    .ToList()))
            .ToList();
    }
}
