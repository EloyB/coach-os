using ClosedXML.Excel;
using CoachOS.Application.Export;
using CoachOS.Infrastructure.Export;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Export;

[TestFixture]
public class ClosedXmlCampWorkbookBuilderTests
{
    private readonly ClosedXmlCampWorkbookBuilder _builder = new();

    [Test]
    public void Build_CreatesEnrollmentsAndDaysSheets()
    {
        using var stream = new MemoryStream(_builder.Build(BuildModel()));
        using var workbook = new XLWorkbook(stream);

        workbook.Worksheets.Select(w => w.Name).Should().Equal("Inschrijvingen", "Kampdagen");
    }

    [Test]
    public void Build_EnrollmentsSheet_HasPaymentColumnsAndFormAnswers()
    {
        using var stream = new MemoryStream(_builder.Build(BuildModel()));
        using var workbook = new XLWorkbook(stream);
        IXLWorksheet ws = workbook.Worksheet("Inschrijvingen");

        ws.Cell(3, 1).GetString().Should().Be("Naam");
        ws.Cell(3, 5).GetString().Should().Be("Betaalstatus");
        ws.Cell(3, 9).GetString().Should().Be("Dagen");
        ws.Cell(3, 10).GetString().Should().Be("Bedrag");
        ws.Cell(3, 11).GetString().Should().Be("Shirtmaat");

        ws.Cell(4, 1).GetString().Should().Contain("Individuele inschrijvingen");
        ws.Cell(5, 1).GetString().Should().Be("Emma");
        ws.Cell(5, 5).GetString().Should().Be("Betaald");
        ws.Cell(5, 6).GetString().Should().Be("Online");
        ws.Cell(5, 9).GetString().Should().Be("06/07 hele dag");
        ws.Cell(5, 10).GetDouble().Should().Be(32.5);
        ws.Cell(5, 11).GetString().Should().Be("M");
    }

    [Test]
    public void Build_DaysSheet_ListsDayWithTrainers()
    {
        using var stream = new MemoryStream(_builder.Build(BuildModel()));
        using var workbook = new XLWorkbook(stream);
        IXLWorksheet ws = workbook.Worksheet("Kampdagen");

        ws.Cell(4, 1).GetString().Should().Be("Maandag");
        ws.Cell(3, 3).GetString().Should().Be("Voormiddag");
        ws.Cell(3, 4).GetString().Should().Be("Namiddag");
        ws.Cell(4, 3).GetString().Should().Be("09:15–12:00");
        ws.Cell(4, 4).GetString().Should().Be("13:15–16:00");
        ws.Cell(4, 5).GetDouble().Should().Be(3);
        ws.Cell(4, 6).GetDouble().Should().Be(2);
        ws.Cell(4, 7).GetString().Should().Contain("Jan Janssen").And.Contain("Piet (09:00–12:00)");
    }

    private static CampExportModel BuildModel() => new()
    {
        CampName = "Zomerkamp",
        StartDate = new DateOnly(2026, 7, 6),
        EndDate = new DateOnly(2026, 7, 10),
        FormFieldLabels = ["Shirtmaat"],
        Enrollments =
        [
            new CampEnrollmentRow(
                "Emma", "Individueel", null, "emma@test.com", "0470123456", "Bevestigd",
                "Betaald", "Online", new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc), null,
                new Dictionary<string, string> { ["Shirtmaat"] = "M" }, "06/07 hele dag", 32.5m),
        ],
        Days =
        [
            new CampDayRow(
                new DateOnly(2026, 7, 6), "Maandag",
                new TimeOnly(9, 15), new TimeOnly(12, 0), new TimeOnly(13, 15), new TimeOnly(16, 0), 3, 2,
                [
                    new CampDayTrainerRow("Jan Janssen", new TimeOnly(9, 15), new TimeOnly(16, 0)),
                    new CampDayTrainerRow("Piet", new TimeOnly(9, 0), new TimeOnly(12, 0)),
                ]),
        ],
    };
}
