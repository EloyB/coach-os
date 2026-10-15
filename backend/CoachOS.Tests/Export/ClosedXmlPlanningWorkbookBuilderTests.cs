using ClosedXML.Excel;
using CoachOS.Application.Export;
using CoachOS.Infrastructure.Export;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Export;

[TestFixture]
public class ClosedXmlPlanningWorkbookBuilderTests
{
    private readonly ClosedXmlPlanningWorkbookBuilder _builder = new();

    [Test]
    public void Build_ProducesReopenableWorkbookWithThreeNamedSheets()
    {
        PlanningExportModel model = BuildModel();

        byte[] bytes = _builder.Build(model);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);

        workbook.Worksheets.Select(w => w.Name).Should().Equal(
            "Inschrijvingen", "Lesmomenten", "Indeling per lesmoment");
    }

    [Test]
    public void Build_EmbedsBrandLogoOnEverySheet()
    {
        PlanningExportModel model = BuildModel();

        using var stream = new MemoryStream(_builder.Build(model));
        using var workbook = new XLWorkbook(stream);

        // Eén logo per tabblad — faalt als de embedded-resource-naam niet klopt
        // (dan zou het logo stil weggelaten worden).
        workbook.Worksheets.Should().OnlyContain(w => w.Pictures.Count == 1);
    }

    [Test]
    public void Build_EnrollmentsSheet_HasFixedHeadersPlusCustomFieldColumns()
    {
        PlanningExportModel model = BuildModel();

        using var stream = new MemoryStream(_builder.Build(model));
        using var workbook = new XLWorkbook(stream);
        IXLWorksheet ws = workbook.Worksheet("Inschrijvingen");

        // Rij 1 = banner, rij 2 = subtitel, rij 3 = koppen. Vaste koppen: Naam, E-mail,
        // Telefoon, Status, Ingeschreven op, Notities (6) + custom veld "Niveau" (kolom 7).
        // Daarna een groep-kopbalk (rij 4) en de leden eronder (rij 5).
        ws.Cell(3, 1).GetString().Should().Be("Naam");
        ws.Cell(3, 7).GetString().Should().Be("Niveau");
        ws.Cell(4, 1).GetString().Should().Contain("Groep A").And.Contain("leider: Alice");
        ws.Cell(5, 1).GetString().Should().Be("Alice");
        ws.Cell(5, 7).GetString().Should().Be("Gevorderd");
    }

    [Test]
    public void Build_LessonMomentsSheet_GroupsSlotsUnderADayHeader()
    {
        PlanningExportModel model = BuildModel();

        using var stream = new MemoryStream(_builder.Build(model));
        using var workbook = new XLWorkbook(stream);
        IXLWorksheet ws = workbook.Worksheet("Lesmomenten");

        // Banner (1) + subtitel (2) + kopregel (3) + dag-kopbalk (4) + 1 slot (5).
        ws.LastRowUsed()!.RowNumber().Should().Be(5);
        ws.Cell(3, 1).GetString().Should().Be("Van");
        ws.Cell(4, 1).GetString().Should().Be("Maandag 04/05/2026"); // dag-kopbalk
        ws.Cell(5, 1).GetString().Should().Be("09:00");              // starttijd
        ws.Cell(5, 3).GetString().Should().Be("Jan Janssen");        // trainer-kolom
        ws.Cell(5, 4).GetString().Should().Be("Baan 1");             // baan-kolom
    }

    [Test]
    public void Build_MomentRosterSheet_WritesMomentHeaderThenPlayers()
    {
        PlanningExportModel model = BuildModel();

        using var stream = new MemoryStream(_builder.Build(model));
        using var workbook = new XLWorkbook(stream);
        IXLWorksheet ws = workbook.Worksheet("Indeling per lesmoment");

        // Banner (1) + subtitel (2) + kopregel (3) + moment-kop (4) + 1 speler (5).
        ws.Cell(3, 1).GetString().Should().Be("Speler");
        ws.Cell(4, 1).GetString().Should().Contain("Maandag").And.Contain("1/4");
        ws.Cell(5, 1).GetString().Should().Be("Tom");
        ws.Cell(5, 2).GetString().Should().Be("Groep A");
        ws.Cell(5, 3).GetString().Should().Be("Bevestigd");
    }

    private static PlanningExportModel BuildModel() => new()
    {
        SeriesName = "Voorjaarsreeks",
        FormFieldLabels = ["Niveau"],
        MomentRosters =
        [
            new MomentRosterRow(
                "Maandag", new TimeOnly(9, 0), new TimeOnly(10, 0),
                "Jan Janssen", "Baan 1", 4,
                [new RosterPlayer("Tom", "Groep A", "Bevestigd")]),
        ],
        Enrollments =
        [
            new EnrollmentRow(
                "Alice", "Groepsleider", "Groep A", "alice@test.com", "0470123456", "Bevestigd",
                new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc), "nota",
                new Dictionary<string, string> { ["Niveau"] = "Gevorderd" }),
        ],
        LessonMoments =
        [
            new LessonMomentRow(
                new DateOnly(2026, 5, 4), "Maandag",
                new TimeOnly(9, 0), new TimeOnly(10, 0), "Jan Janssen", "Baan 1", 4),
        ],
    };
}
