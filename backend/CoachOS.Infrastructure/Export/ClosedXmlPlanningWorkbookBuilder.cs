using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
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

    private const int BannerRow = 1;
    private const int SubtitleRow = 2;
    private const int HeaderRow = 3;
    private const int FirstDataRow = 4;

    // Huisstijl (zie CLAUDE.md — tennis brand tokens).
    private static readonly XLColor TennisGreen = XLColor.FromHtml("#2D5016");
    private static readonly XLColor TennisLime = XLColor.FromHtml("#D0FF14");
    private static readonly XLColor OffWhite = XLColor.FromHtml("#FAFAF8");
    private static readonly XLColor SubtitleGray = XLColor.FromHtml("#6B7280");
    private static readonly XLColor GridLine = XLColor.FromHtml("#E5E3DE");
    private static readonly XLColor MomentHeaderFill = XLColor.FromHtml("#EDF3E3");

    private static readonly byte[]? LogoBytes = LoadLogoBytes();

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
        RenderShell(ws, "Inschrijvingen", model, headers);

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
            WriteBandHeader(ws, row, headers.Length, band);
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

        FinalizeGrouped(ws, headers.Length, row - 1);
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
        RenderShell(ws, "Lesmomenten", model, headers);

        int row = FirstDataRow;
        DateOnly? currentDate = null;
        foreach (LessonMomentRow m in model.LessonMoments) // al gesorteerd op datum + starttijd
        {
            if (currentDate != m.Date)
            {
                currentDate = m.Date;
                WriteBandHeader(ws, row, headers.Length, $"{m.DayName} {m.Date:dd/MM/yyyy}");
                row++;
            }

            ws.Cell(row, 1).Value = m.StartTime.ToString(TimeFormat);
            ws.Cell(row, 2).Value = m.EndTime.ToString(TimeFormat);
            ws.Cell(row, 3).Value = m.TrainerName ?? string.Empty;
            ws.Cell(row, 4).Value = m.CourtName ?? string.Empty;
            ws.Cell(row, 5).Value = m.MaxStudents;
            row++;
        }

        FinalizeGrouped(ws, headers.Length, row - 1);
    }

    private static void BuildMomentRosterSheet(XLWorkbook workbook, PlanningExportModel model)
    {
        IXLWorksheet ws = workbook.Worksheets.Add("Indeling per lesmoment");

        string[] headers = ["Speler", "Groep", "Status"];
        RenderShell(ws, "Indeling per lesmoment", model, headers);

        int row = FirstDataRow;
        foreach (MomentRosterRow m in model.MomentRosters)
        {
            string extra = string.Empty;
            if (!string.IsNullOrWhiteSpace(m.CourtName)) extra += $"  ·  {m.CourtName}";
            if (!string.IsNullOrWhiteSpace(m.TrainerName)) extra += $"  ·  {m.TrainerName}";

            WriteBandHeader(ws, row, headers.Length,
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

        FinalizeGrouped(ws, headers.Length, row - 1);
    }

    /// <summary>Gekleurde, samengevoegde kopbalk die een groep rijen inleidt.</summary>
    private static void WriteBandHeader(IXLWorksheet ws, int row, int columnCount, string text)
    {
        ws.Range(row, 1, row, columnCount).Merge().Style.Fill.BackgroundColor = MomentHeaderFill;

        IXLCell cell = ws.Cell(row, 1);
        cell.Value = text;
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontColor = TennisGreen;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        cell.Style.Alignment.Indent = 1;
        ws.Row(row).Height = 18;
    }

    /// <summary>
    /// Afronding voor gegroepeerde tabbladen: geen zebra (de kopbalken scheiden de
    /// groepen al), wel randen + bevroren kop + autofit.
    /// </summary>
    private static void FinalizeGrouped(IXLWorksheet ws, int columnCount, int lastRow)
    {
        int last = Math.Max(lastRow, HeaderRow);
        IXLRange table = ws.Range(HeaderRow, 1, last, columnCount);
        table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        table.Style.Border.InsideBorderColor = GridLine;
        table.Style.Border.OutsideBorderColor = GridLine;
        ws.SheetView.FreezeRows(HeaderRow);
        ws.Columns(1, columnCount).AdjustToContents(HeaderRow, last);
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
