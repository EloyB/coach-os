using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using CoachOS.Application.Export;

namespace CoachOS.Infrastructure.Export;

/// <summary>
/// Bouwt de Excel-werkmap voor een lessenreeks-planning met ClosedXML, in de
/// huisstijl van de app: tennis-green banner met logo, gekleurde koppen,
/// zebra-striping en bevroren kop. Drie tabbladen: Inschrijvingen, Lesmomenten,
/// Geplande lessen.
/// </summary>
public class ClosedXmlPlanningWorkbookBuilder : IPlanningWorkbookBuilder
{
    private const string DateFormat = "dd/MM/yyyy";
    private const string DateTimeFormat = "dd/MM/yyyy HH:mm";
    private const string TimeFormat = "HH:mm";

    private const int BannerRow = 1;
    private const int SubtitleRow = 2;
    private const int HeaderRow = 3;
    private const int FirstDataRow = 4;

    // Huisstijl (zie CLAUDE.md — tennis brand tokens).
    private static readonly XLColor TennisGreen = XLColor.FromHtml("#2D5016");
    private static readonly XLColor TennisLime = XLColor.FromHtml("#D0FF14");
    private static readonly XLColor OffWhite = XLColor.FromHtml("#FAFAF8");
    private static readonly XLColor ZebraTint = XLColor.FromHtml("#F5F4F1");
    private static readonly XLColor SubtitleGray = XLColor.FromHtml("#6B7280");
    private static readonly XLColor GridLine = XLColor.FromHtml("#E5E3DE");

    private static readonly byte[]? LogoBytes = LoadLogoBytes();

    public byte[] Build(PlanningExportModel model)
    {
        using XLWorkbook workbook = new();

        BuildEnrollmentsSheet(workbook, model);
        BuildLessonMomentsSheet(workbook, model);
        BuildScheduledSheet(workbook, model);

        using MemoryStream stream = new();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void BuildEnrollmentsSheet(XLWorkbook workbook, PlanningExportModel model)
    {
        IXLWorksheet ws = workbook.Worksheets.Add("Inschrijvingen");

        string[] fixedHeaders = ["Naam", "E-mail", "Telefoon", "Status", "Ingeschreven op", "Notities"];
        string[] headers = [.. fixedHeaders, .. model.FormFieldLabels];
        RenderShell(ws, "Inschrijvingen", model, headers);

        int row = FirstDataRow;
        foreach (EnrollmentRow e in model.Enrollments)
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

        Finalize(ws, headers.Length, row - 1);
    }

    private static void BuildLessonMomentsSheet(XLWorkbook workbook, PlanningExportModel model)
    {
        IXLWorksheet ws = workbook.Worksheets.Add("Lesmomenten");

        string[] headers = ["Datum", "Dag", "Van", "Tot", "Trainer", "Baan", "Max"];
        RenderShell(ws, "Lesmomenten", model, headers);

        int row = FirstDataRow;
        foreach (LessonMomentRow m in model.LessonMoments)
        {
            IXLCell dateCell = ws.Cell(row, 1);
            dateCell.Value = m.Date.ToDateTime(TimeOnly.MinValue);
            dateCell.Style.DateFormat.Format = DateFormat;
            ws.Cell(row, 2).Value = m.DayName;
            ws.Cell(row, 3).Value = m.StartTime.ToString(TimeFormat);
            ws.Cell(row, 4).Value = m.EndTime.ToString(TimeFormat);
            ws.Cell(row, 5).Value = m.TrainerName ?? string.Empty;
            ws.Cell(row, 6).Value = m.CourtName ?? string.Empty;
            ws.Cell(row, 7).Value = m.MaxStudents;
            row++;
        }

        Finalize(ws, headers.Length, row - 1);
    }

    private static void BuildScheduledSheet(XLWorkbook workbook, PlanningExportModel model)
    {
        IXLWorksheet ws = workbook.Worksheets.Add("Geplande lessen");

        string[] headers = ["Datum", "Van", "Tot", "Speler", "E-mail", "Groep", "Status"];
        RenderShell(ws, "Geplande lessen", model, headers);

        int row = FirstDataRow;
        foreach (ScheduledRow s in model.ScheduledLessons)
        {
            IXLCell dateCell = ws.Cell(row, 1);
            dateCell.Value = s.Date.ToDateTime(TimeOnly.MinValue);
            dateCell.Style.DateFormat.Format = DateFormat;
            ws.Cell(row, 2).Value = s.StartTime.ToString(TimeFormat);
            ws.Cell(row, 3).Value = s.EndTime.ToString(TimeFormat);
            ws.Cell(row, 4).Value = s.StudentName;
            ws.Cell(row, 5).Value = s.StudentEmail;
            ws.Cell(row, 6).Value = s.GroupName ?? string.Empty;
            ws.Cell(row, 7).Value = s.Status;
            row++;
        }

        Finalize(ws, headers.Length, row - 1);
    }

    /// <summary>Schrijft de banner (rij 1), subtitel (rij 2) en kopregel (rij 3).</summary>
    private static void RenderShell(IXLWorksheet ws, string sheetTitle, PlanningExportModel model, string[] headers)
    {
        int cols = headers.Length;

        // Banner met merknaam + tabbladtitel.
        IXLRange banner = ws.Range(BannerRow, 1, BannerRow, cols).Merge();
        banner.Style.Fill.BackgroundColor = TennisGreen;
        ws.Row(BannerRow).Height = 38;

        IXLCell bannerCell = ws.Cell(BannerRow, 1);
        IXLRichText title = bannerCell.CreateRichText();
        title.AddText("CoachOS").SetFontColor(TennisLime).SetBold(true).SetFontSize(15);
        title.AddText("   " + sheetTitle).SetFontColor(XLColor.White).SetFontSize(11);
        bannerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        bannerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        bannerCell.Style.Alignment.Indent = 5; // ruimte vrijhouden voor het logo

        // Subtitel: reeksnaam + exportdatum.
        IXLRange subtitle = ws.Range(SubtitleRow, 1, SubtitleRow, cols).Merge();
        subtitle.Style.Fill.BackgroundColor = OffWhite;
        ws.Row(SubtitleRow).Height = 18;

        IXLCell subtitleCell = ws.Cell(SubtitleRow, 1);
        subtitleCell.Value = $"{model.SeriesName}  ·  Geëxporteerd op {model.ExportedOn:dd/MM/yyyy}";
        subtitleCell.Style.Font.FontColor = SubtitleGray;
        subtitleCell.Style.Font.FontSize = 9;
        subtitleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        subtitleCell.Style.Alignment.Indent = 1;

        // Logo over de banner (degradeert netjes als de resource ontbreekt).
        if (LogoBytes is not null)
        {
            using MemoryStream logo = new(LogoBytes);
            ws.AddPicture(logo, XLPictureFormat.Png)
                .MoveTo(ws.Cell(BannerRow, 1), 6, 6)
                .WithSize(26, 26);
        }

        // Kopregel.
        for (int i = 0; i < cols; i++)
        {
            IXLCell cell = ws.Cell(HeaderRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = TennisGreen;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        ws.Row(HeaderRow).Height = 20;
    }

    private static void Finalize(IXLWorksheet ws, int columnCount, int lastDataRow)
    {
        // Zebra-striping op de datarijen voor leesbaarheid.
        for (int r = FirstDataRow; r <= lastDataRow; r++)
        {
            if ((r - FirstDataRow) % 2 == 1)
                ws.Range(r, 1, r, columnCount).Style.Fill.BackgroundColor = ZebraTint;
        }

        // Subtiele randen rond kop + data.
        int tableLastRow = Math.Max(lastDataRow, HeaderRow);
        IXLRange table = ws.Range(HeaderRow, 1, tableLastRow, columnCount);
        table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        table.Style.Border.InsideBorderColor = GridLine;
        table.Style.Border.OutsideBorderColor = GridLine;

        ws.SheetView.FreezeRows(HeaderRow);

        // Kolombreedte op basis van kop + data (niet de samengevoegde banner).
        ws.Columns(1, columnCount).AdjustToContents(HeaderRow, tableLastRow);
    }

    private static byte[]? LoadLogoBytes()
    {
        using Stream? stream = typeof(ClosedXmlPlanningWorkbookBuilder).Assembly
            .GetManifestResourceStream("CoachOS.Infrastructure.Export.Assets.coachos-logo.png");
        if (stream is null)
            return null;

        using MemoryStream ms = new();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
