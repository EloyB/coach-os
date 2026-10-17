using ClosedXML.Excel;
using CoachOS.Application.Export;

namespace CoachOS.Infrastructure.Export;

/// <summary>
/// Bouwt de Excel-werkmap voor een kamp met ClosedXML, in dezelfde huisstijl als de
/// lessenreeks-export. Twee tabbladen: Inschrijvingen (individuen en groepen onder
/// eigen kopbalk, incl. betaalstatus + formulier-antwoorden) en Kampdagen.
/// </summary>
public class ClosedXmlCampWorkbookBuilder : ICampWorkbookBuilder
{
    private const string DateTimeFormat = "dd/MM/yyyy HH:mm";
    private const string TimeFormat = "HH:mm";

    private const int FirstDataRow = ExportWorkbookStyle.FirstDataRow;

    public byte[] Build(CampExportModel model)
    {
        using XLWorkbook workbook = new();

        BuildEnrollmentsSheet(workbook, model);
        BuildDaysSheet(workbook, model);

        using MemoryStream stream = new();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void BuildEnrollmentsSheet(XLWorkbook workbook, CampExportModel model)
    {
        IXLWorksheet ws = workbook.Worksheets.Add("Inschrijvingen");

        string[] fixedHeaders =
            ["Naam", "E-mail", "Telefoon", "Status", "Betaalstatus", "Betaalmethode", "Ingeschreven op", "Notities"];
        string[] headers = [.. fixedHeaders, .. model.FormFieldLabels];
        ExportWorkbookStyle.RenderShell(ws, "Inschrijvingen", model.CampName, model.ExportedOn, headers);

        // Rijen zijn al gesorteerd (individuen eerst, dan groepen met de leider bovenaan),
        // dus GroupBy behoudt die volgorde.
        int row = FirstDataRow;
        foreach (IGrouping<string?, CampEnrollmentRow> section in model.Enrollments.GroupBy(e => e.GroupName))
        {
            int count = section.Count();
            string band = section.Key is null
                ? $"Individuele inschrijvingen  ·  {count}"
                : $"{section.Key}  ·  leider: {GroupLeaderName(section)}  ·  {count} {(count == 1 ? "lid" : "leden")}";
            ExportWorkbookStyle.WriteBandHeader(ws, row, headers.Length, band);
            row++;

            foreach (CampEnrollmentRow e in section)
            {
                int col = 1;
                ws.Cell(row, col++).Value = e.ParticipantName;
                ws.Cell(row, col++).Value = e.Email;
                ws.Cell(row, col++).Value = e.Phone ?? string.Empty;
                ws.Cell(row, col++).Value = e.Status;
                ws.Cell(row, col++).Value = e.PaymentStatus ?? string.Empty;
                ws.Cell(row, col++).Value = e.PaymentMethod ?? string.Empty;
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

    private static string GroupLeaderName(IEnumerable<CampEnrollmentRow> members)
        => (members.FirstOrDefault(m => m.EnrollmentType == "Groepsleider")
            ?? members.First()).ParticipantName;

    private static void BuildDaysSheet(XLWorkbook workbook, CampExportModel model)
    {
        IXLWorksheet ws = workbook.Worksheets.Add("Kampdagen");

        string[] headers = ["Dag", "Datum", "Van", "Tot", "Trainers"];
        ExportWorkbookStyle.RenderShell(ws, "Kampdagen", model.CampName, model.ExportedOn, headers);

        int row = FirstDataRow;
        foreach (CampDayRow d in model.Days) // al gesorteerd op datum + starttijd
        {
            ws.Cell(row, 1).Value = d.DayName;
            IXLCell dateCell = ws.Cell(row, 2);
            dateCell.Value = d.Date.ToDateTime(TimeOnly.MinValue);
            dateCell.Style.DateFormat.Format = "dd/MM/yyyy";
            ws.Cell(row, 3).Value = d.StartTime.ToString(TimeFormat);
            ws.Cell(row, 4).Value = d.EndTime.ToString(TimeFormat);
            ws.Cell(row, 5).Value = string.Join(", ", d.Trainers.Select(t =>
                t.StartTime == d.StartTime && t.EndTime == d.EndTime
                    ? t.Name
                    : $"{t.Name} ({t.StartTime.ToString(TimeFormat)}–{t.EndTime.ToString(TimeFormat)})"));
            row++;
        }

        ExportWorkbookStyle.FinalizeGrouped(ws, headers.Length, row - 1);
    }
}
