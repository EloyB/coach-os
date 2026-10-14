using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;

namespace CoachOS.Infrastructure.Export;

/// <summary>
/// Gedeelde huisstijl voor alle Excel-exports: tennis-green banner met logo,
/// gekleurde koppen, kopbalken per groep en bevroren kop.
/// </summary>
internal static class ExportWorkbookStyle
{
    internal const int BannerRow = 1;
    internal const int SubtitleRow = 2;
    internal const int HeaderRow = 3;
    internal const int FirstDataRow = 4;

    // Huisstijl (zie CLAUDE.md — tennis brand tokens).
    private static readonly XLColor TennisGreen = XLColor.FromHtml("#2D5016");
    private static readonly XLColor TennisLime = XLColor.FromHtml("#D0FF14");
    private static readonly XLColor OffWhite = XLColor.FromHtml("#FAFAF8");
    private static readonly XLColor SubtitleGray = XLColor.FromHtml("#6B7280");
    private static readonly XLColor GridLine = XLColor.FromHtml("#E5E3DE");
    private static readonly XLColor MomentHeaderFill = XLColor.FromHtml("#EDF3E3");

    private static readonly byte[]? LogoBytes = LoadLogoBytes();

    /// <summary>Gekleurde, samengevoegde kopbalk die een groep rijen inleidt.</summary>
    internal static void WriteBandHeader(IXLWorksheet ws, int row, int columnCount, string text)
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
    internal static void FinalizeGrouped(IXLWorksheet ws, int columnCount, int lastRow)
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
    internal static void RenderShell(IXLWorksheet ws, string sheetTitle, string subtitleName, DateOnly exportedOn, string[] headers)
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
        subtitleCell.Value = $"{subtitleName}  ·  Geëxporteerd op {exportedOn:dd/MM/yyyy}";
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
        using Stream? stream = typeof(ExportWorkbookStyle).Assembly
            .GetManifestResourceStream("CoachOS.Infrastructure.Export.Assets.coachos-logo.png");
        if (stream is null)
            return null;

        using MemoryStream ms = new();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
