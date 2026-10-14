using ClosedXML.Excel;
using CoachOS.Application.Export;

namespace CoachOS.Infrastructure.Export;

/// <summary>
/// Bouwt de Excel-werkmap voor een lessenreeks-planning met ClosedXML, in de
/// huisstijl van de app: tennis-green banner met logo, gekleurde koppen,
/// zebra-striping en bevroren kop. Drie tabbladen: Inschrijvingen, Lesmomenten
/// (per dag gegroepeerd) en Indeling per lesmoment.
/// </summary>
public class ClosedXmlPlanningWorkbookBuilder : IPlanningWorkbookBuilder
{
    private const string DateTimeFormat = "dd/MM/yyyy HH:mm";
    private const string TimeFormat = "HH:mm";

    private const int FirstDataRow = ExportWorkbookStyle.FirstDataRow;
    private const int HeaderRow = ExportWorkbookStyle.HeaderRow;

    public byte[] Build(PlanningExportModel model)
    {
        using XLWorkbook workbook = new();

        BuildEnrollmentsSheet(workbook, model);
        BuildLessonMomentsSheet(workbook, model);
        BuildMomentRosterSheet(workbook, model);

        using MemoryStream stream = new();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void BuildEnrollmentsSheet(XLWorkbook workbook, PlanningExportModel model)
    {
        IXLWorksheet ws = workbook.Worksheets.Add("Inschrijvingen");

        string[] fixedHeaders = ["Naam", "E-mail", "Telefoon", "Status", "Ingeschreven op", "Notities"];
        string[] headers = [.. fixedHeaders, .. model.FormFieldLabels];
        ExportWorkbookStyle.RenderShell(ws, "Inschrijvingen", model.SeriesName, model.ExportedOn, headers);

        // Individuen en groepen elk onder een eigen kopbalk. De rijen zijn al zo
        // gesorteerd (individuen eerst, dan groepen geclusterd met de leider bovenaan),
        // dus GroupBy behoudt die volgorde.
        int row = FirstDataRow;
        foreach (IGrouping<string?, EnrollmentRow> section in model.Enrollments.GroupBy(e => e.GroupName))
        {
            int count = section.Count();
            string band = section.Key is null
                ? $"Individuele inschrijvingen  ·  {count}"
                : $"{section.Key}  ·  leider: {GroupLeaderName(section)}  ·  {count} {(count == 1 ? "lid" : "leden")}";
            ExportWorkbookStyle.WriteBandHeader(ws, row, headers.Length, band);
            row++;

            foreach (EnrollmentRow e in section)
            {
                int col = 1;
                ws.Cell(row, col++).Value = e.StudentName;
                ws.Cell(row, col++).Value = e.StudentEmail;
                ws.Cell(row, col++).Value = e.StudentPhone ?? string.Empty;
                ws.Cell(row, col++).Value = e.Status;
                IXLCell enrolledCell = ws.Cell(row, col++);
                enrolledCell.Value = e.EnrolledAt;
                enrolledCell.Style.DateFormat.Format = DateTimeFormat;
                ws.Cell(row, col++).Value = e.Notes ?? string.Empty;

                foreach (string label in model.FormFieldLabels)
                    ws.Cell(row, col++).Value = e.FormResponses.TryGetValue(label, out string? value) ? value : string.Empty;

                row++;
            }
        }

        ExportWorkbookStyle.FinalizeGrouped(ws, headers.Length, row - 1);
    }

    private static string GroupLeaderName(IEnumerable<EnrollmentRow> members)
        => (members.FirstOrDefault(m => m.EnrollmentType == "Groepsleider")
            ?? members.First()).StudentName;

    private static void BuildLessonMomentsSheet(XLWorkbook workbook, PlanningExportModel model)
    {
        IXLWorksheet ws = workbook.Worksheets.Add("Lesmomenten");

        // Gegroepeerd per dag: een gekleurde dag-kopbalk per datum, met de tijdsloten
        // eronder. Datum + dag verhuizen naar de kopbalk zodat meerdere slots op
        // dezelfde dag duidelijk bij elkaar horen.
        string[] headers = ["Van", "Tot", "Trainer", "Baan", "Max"];
        ExportWorkbookStyle.RenderShell(ws, "Lesmomenten", model.SeriesName, model.ExportedOn, headers);

        int row = FirstDataRow;
        DateOnly? currentDate = null;
        foreach (LessonMomentRow m in model.LessonMoments) // al gesorteerd op datum + starttijd
        {
            if (currentDate != m.Date)
            {
                currentDate = m.Date;
                ExportWorkbookStyle.WriteBandHeader(ws, row, headers.Length, $"{m.DayName} {m.Date:dd/MM/yyyy}");
                row++;
            }

            ws.Cell(row, 1).Value = m.StartTime.ToString(TimeFormat);
            ws.Cell(row, 2).Value = m.EndTime.ToString(TimeFormat);
            ws.Cell(row, 3).Value = m.TrainerName ?? string.Empty;
            ws.Cell(row, 4).Value = m.CourtName ?? string.Empty;
            ws.Cell(row, 5).Value = m.MaxStudents;
            row++;
        }

        ExportWorkbookStyle.FinalizeGrouped(ws, headers.Length, row - 1);
    }

    private static void BuildMomentRosterSheet(XLWorkbook workbook, PlanningExportModel model)
    {
        IXLWorksheet ws = workbook.Worksheets.Add("Indeling per lesmoment");

        string[] headers = ["Speler", "Groep", "Status"];
        ExportWorkbookStyle.RenderShell(ws, "Indeling per lesmoment", model.SeriesName, model.ExportedOn, headers);

        int row = FirstDataRow;
        foreach (MomentRosterRow m in model.MomentRosters)
        {
            string extra = string.Empty;
            if (!string.IsNullOrWhiteSpace(m.CourtName)) extra += $"  ·  {m.CourtName}";
            if (!string.IsNullOrWhiteSpace(m.TrainerName)) extra += $"  ·  {m.TrainerName}";

            ExportWorkbookStyle.WriteBandHeader(ws, row, headers.Length,
                $"{m.DayName} {m.StartTime:HH\\:mm}–{m.EndTime:HH\\:mm}{extra}  ·  {m.Players.Count}/{m.MaxStudents}");
            row++;

            foreach (RosterPlayer p in m.Players)
            {
                ws.Cell(row, 1).Value = p.Name;
                ws.Cell(row, 2).Value = p.GroupName ?? string.Empty;
                ws.Cell(row, 3).Value = p.Status;
                row++;
            }
        }

        ExportWorkbookStyle.FinalizeGrouped(ws, headers.Length, row - 1);
    }
}
