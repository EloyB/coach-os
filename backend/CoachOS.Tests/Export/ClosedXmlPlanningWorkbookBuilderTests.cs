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
            "Inschrijvingen", "Lesmomenten", "Geplande lessen");
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

        // Rij 1 = banner, rij 2 = subtitel, rij 3 = koppen, rij 4 = eerste datarij.
        ws.Cell(3, 1).GetString().Should().Be("Naam");
        ws.Cell(3, 7).GetString().Should().Be("Niveau"); // custom form field column
        ws.Cell(4, 1).GetString().Should().Be("Alice");
        ws.Cell(4, 7).GetString().Should().Be("Gevorderd");
    }

    [Test]
    public void Build_LessonMomentsSheet_WritesOneRowPerMoment()
    {
        PlanningExportModel model = BuildModel();

        using var stream = new MemoryStream(_builder.Build(model));
        using var workbook = new XLWorkbook(stream);
        IXLWorksheet ws = workbook.Worksheet("Lesmomenten");

        // Banner (1) + subtitel (2) + kopregel (3) + 1 lesmoment (4).
        ws.LastRowUsed()!.RowNumber().Should().Be(4);
        ws.Cell(3, 1).GetString().Should().Be("Datum");
        ws.Cell(4, 2).GetString().Should().Be("Maandag");
    }

    private static PlanningExportModel BuildModel() => new()
    {
        SeriesName = "Voorjaarsreeks",
        FormFieldLabels = ["Niveau"],
        Enrollments =
        [
            new EnrollmentRow(
                "Alice", "alice@test.com", "0470123456", "Bevestigd",
                new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc), "nota",
                new Dictionary<string, string> { ["Niveau"] = "Gevorderd" }),
        ],
        LessonMoments =
        [
            new LessonMomentRow(
                new DateOnly(2026, 5, 4), "Maandag",
                new TimeOnly(9, 0), new TimeOnly(10, 0), "Jan Janssen", "Baan 1", 4),
        ],
        ScheduledLessons =
        [
            new ScheduledRow(
                new DateOnly(2026, 5, 4), new TimeOnly(9, 0), new TimeOnly(10, 0),
                "Tom", "tom@test.com", "Groep A", "Bevestigd"),
        ],
    };
}
