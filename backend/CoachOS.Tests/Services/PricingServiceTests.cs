using CoachOS.Application.Pricing;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class PricingServiceTests
{
    private Mock<ILessonSerieRepository> _series = null!;
    private Mock<ILessonSeriePriceRepository> _prices = null!;
    private PricingService _sut = null!;

    private static readonly Guid SeriesId = Guid.NewGuid();
    private static readonly Guid OrgId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _series = new Mock<ILessonSerieRepository>();
        _prices = new Mock<ILessonSeriePriceRepository>();
        _sut = new PricingService(_series.Object, _prices.Object);
    }

    private void ArrangeSeries(decimal legacyPrice)
    {
        _series.Setup(s => s.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LessonSerie
            {
                Id = SeriesId,
                OrganizationId = OrgId,
                Name = "Voorjaarsreeks",
                Price = legacyPrice,
            });
    }

    private void ArrangeMatrix(params (ParticipantCategory Category, int GroupSize, decimal Total)[] rows)
    {
        List<LessonSeriePrice> matrix = rows.Select(r => new LessonSeriePrice
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            Label = r.Category == ParticipantCategory.Youth ? "Jeugd" : "Volwassenen",
            Mode = PricingMode.GroupSize,
            Category = r.Category,
            GroupSize = r.GroupSize,
            TotalPrice = r.Total,
        }).ToList();

        _prices.Setup(p => p.GetBySeriesPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(matrix);
    }

    private void ArrangeOptions(params LessonSeriePrice[] rows)
    {
        _prices.Setup(p => p.GetBySeriesPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows.ToList());
    }

    private static Enrollment Participant(ParticipantCategory? category) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = OrgId,
        LessonSerieId = SeriesId,
        StudentName = "Deelnemer",
        StudentEmail = $"{Guid.NewGuid():N}@test.be",
        Category = category,
    };

    private static List<Enrollment> Participants(int adults, int youth)
    {
        List<Enrollment> list = [];
        for (int i = 0; i < adults; i++) list.Add(Participant(ParticipantCategory.Adult));
        for (int i = 0; i < youth; i++) list.Add(Participant(ParticipantCategory.Youth));
        return list;
    }

    // ── Flexibele prijsopties ────────────────────────────────────────────────

    [Test]
    public async Task Calculate_FixedPerParticipantOption_MultipliesByGroupSize()
    {
        ArrangeSeries(legacyPrice: 999m);
        ArrangeOptions(new LessonSeriePrice
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            Label = "Standaardtarief",
            Description = "Voor elke deelnemer hetzelfde bedrag.",
            Mode = PricingMode.FixedPerParticipant,
            TotalPrice = 125m,
        });

        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(
            SeriesId, Participants(adults: 2, youth: 1));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(375m);
        result.Value.Lines.Single().Label.Should().Be("Standaardtarief");
    }

    [Test]
    public async Task Calculate_GroupSizeOption_UsesTotalForWholeGroup()
    {
        ArrangeSeries(legacyPrice: 999m);
        ArrangeOptions(
            new LessonSeriePrice { Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = SeriesId, Label = "Duo", Mode = PricingMode.GroupSize, GroupSize = 2, TotalPrice = 260m },
            new LessonSeriePrice { Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = SeriesId, Label = "Groep van drie", Mode = PricingMode.GroupSize, GroupSize = 3, TotalPrice = 300m });

        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(
            SeriesId, Participants(adults: 2, youth: 1));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(300m);
        result.Value.Lines.Single().Label.Should().Be("Groep van drie");
    }

    [Test]
    public async Task Calculate_TariffCategoryOptions_ChargePerParticipantCategory()
    {
        ArrangeSeries(legacyPrice: 999m);
        ArrangeOptions(
            new LessonSeriePrice { Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = SeriesId, Label = "Volwassenen", Mode = PricingMode.TariffCategory, Category = ParticipantCategory.Adult, TotalPrice = 140m },
            new LessonSeriePrice { Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = SeriesId, Label = "Jeugd", Mode = PricingMode.TariffCategory, Category = ParticipantCategory.Youth, TotalPrice = 90m });

        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(
            SeriesId, Participants(adults: 1, youth: 2));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(320m);
        result.Value.Lines.Should().HaveCount(2);
    }

    [Test]
    public async Task Calculate_ManualOption_UsesSelectedOptionPerParticipant()
    {
        ArrangeSeries(legacyPrice: 999m);
        Guid socialRateId = Guid.NewGuid();
        ArrangeOptions(new LessonSeriePrice
        {
            Id = socialRateId,
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            Label = "Sociaal tarief",
            Mode = PricingMode.ManualOption,
            TotalPrice = 75m,
        });

        List<Enrollment> participants = Participants(adults: 2, youth: 0);
        participants.ForEach(p => p.SelectedPriceOptionId = socialRateId);

        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(SeriesId, participants);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(150m);
        result.Value.Lines.Single().Label.Should().Be("Sociaal tarief");
    }

    [Test]
    public async Task Calculate_InvalidManualOption_ReturnsValidationError()
    {
        ArrangeSeries(legacyPrice: 999m);
        ArrangeOptions(new LessonSeriePrice
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            Label = "Sociaal tarief",
            Mode = PricingMode.ManualOption,
            TotalPrice = 75m,
        });

        List<Enrollment> participants = Participants(adults: 1, youth: 0);
        participants[0].SelectedPriceOptionId = Guid.NewGuid();

        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(SeriesId, participants);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Validation);
    }

    // ── Legacy fallback ───────────────────────────────────────────────────────

    [Test]
    public async Task Calculate_NoMatrix_FallsBackToLegacyPricePerPerson()
    {
        ArrangeSeries(legacyPrice: 120m);
        ArrangeMatrix(); // lege matrix

        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(
            SeriesId, Participants(adults: 4, youth: 0));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(480m);
        result.Value.UsedLegacyPrice.Should().BeTrue();
    }

    // ── Totaalprijs per groep ─────────────────────────────────────────────────

    [Test]
    public async Task Calculate_HomogeneousGroup_UsesMatrixTotalDirectly()
    {
        // Kern van het gekozen model: de matrix geeft een TOTAAL, niet een prijs
        // per persoon. Een groep van 4 volwassenen betaalt exact €480 — niet 4 × 480.
        ArrangeSeries(legacyPrice: 999m);
        ArrangeMatrix(
            (ParticipantCategory.Adult, 4, 480m),
            (ParticipantCategory.Youth, 4, 360m));

        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(
            SeriesId, Participants(adults: 4, youth: 0));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(480m);
        result.Value.UsedLegacyPrice.Should().BeFalse();
    }

    [Test]
    public async Task Calculate_PrivateLesson_UsesGroupSizeOneRow()
    {
        ArrangeSeries(legacyPrice: 999m);
        ArrangeMatrix(
            (ParticipantCategory.Adult, 4, 480m),
            (ParticipantCategory.Adult, 1, 350m));

        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(
            SeriesId, Participants(adults: 1, youth: 0));

        result.Value!.Total.Should().Be(350m);
    }

    [Test]
    public async Task Calculate_YouthGroup_UsesYouthRow()
    {
        ArrangeSeries(legacyPrice: 999m);
        ArrangeMatrix(
            (ParticipantCategory.Adult, 4, 480m),
            (ParticipantCategory.Youth, 4, 360m));

        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(
            SeriesId, Participants(adults: 0, youth: 4));

        result.Value!.Total.Should().Be(360m);
    }

    // ── Gemengde groep: pro rata ──────────────────────────────────────────────

    [Test]
    public async Task Calculate_MixedGroup_SplitsProRataPerCategory()
    {
        // 2 volwassenen + 2 jeugd in een groep van 4.
        // Volwassen aandeel: 480/4 = 120 pp → 2 × 120 = 240
        // Jeugd aandeel:     360/4 =  90 pp → 2 ×  90 = 180
        // Totaal: 420
        ArrangeSeries(legacyPrice: 999m);
        ArrangeMatrix(
            (ParticipantCategory.Adult, 4, 480m),
            (ParticipantCategory.Youth, 4, 360m));

        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(
            SeriesId, Participants(adults: 2, youth: 2));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(420m);
        result.Value.Lines.Should().HaveCount(2);
        result.Value.Lines.Single(l => l.Category == ParticipantCategory.Adult).Amount.Should().Be(240m);
        result.Value.Lines.Single(l => l.Category == ParticipantCategory.Youth).Amount.Should().Be(180m);
    }

    [Test]
    public async Task Calculate_LinesAlwaysSumToTotal()
    {
        // Bedragen die niet rond deelbaar zijn: 3 deelnemers uit een tarief van 100.
        ArrangeSeries(legacyPrice: 999m);
        ArrangeMatrix(
            (ParticipantCategory.Adult, 3, 100m),
            (ParticipantCategory.Youth, 3, 100m));

        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(
            SeriesId, Participants(adults: 2, youth: 1));

        result.Value!.Lines.Sum(l => l.Amount).Should().Be(result.Value.Total);
    }

    // ── Randgevallen ──────────────────────────────────────────────────────────

    [Test]
    public async Task Calculate_NullCategory_TreatedAsAdult()
    {
        // Inschrijvingen van vóór de tariefcategorieën hebben geen categorie.
        ArrangeSeries(legacyPrice: 999m);
        ArrangeMatrix(
            (ParticipantCategory.Adult, 2, 400m),
            (ParticipantCategory.Youth, 2, 300m));

        List<Enrollment> participants = [Participant(null), Participant(null)];

        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(SeriesId, participants);

        result.Value!.Total.Should().Be(400m);
        result.Value.Lines.Single().Category.Should().Be(ParticipantCategory.Adult);
    }

    [Test]
    public async Task Calculate_GroupSizeNotInMatrix_UsesClosestDefinedSize()
    {
        // Groep van 5, matrix kent alleen 1 t/m 4. Dichtstbij = 4 (€480 → €120 pp),
        // dus 5 × 120 = 600.
        ArrangeSeries(legacyPrice: 999m);
        ArrangeMatrix(
            (ParticipantCategory.Adult, 4, 480m),
            (ParticipantCategory.Adult, 1, 350m));

        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(
            SeriesId, Participants(adults: 5, youth: 0));

        result.Value!.Total.Should().Be(600m);
    }

    [Test]
    public async Task Calculate_CategoryMissingFromMatrix_FallsBackToLegacyForThoseParticipants()
    {
        // Club stelde alleen volwassenentarieven in, maar er schrijft een jeugdlid in.
        ArrangeSeries(legacyPrice: 100m);
        ArrangeMatrix((ParticipantCategory.Adult, 2, 400m));

        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(
            SeriesId, Participants(adults: 1, youth: 1));

        result.IsSuccess.Should().BeTrue();
        // Volwassene: 400/2 = 200. Jeugd: geen rij → legacy 100.
        result.Value!.Total.Should().Be(300m);
    }

    [Test]
    public async Task Calculate_NoParticipants_ReturnsValidationError()
    {
        ArrangeSeries(legacyPrice: 100m);
        ArrangeMatrix();

        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(SeriesId, []);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Validation);
    }

    [Test]
    public async Task Calculate_UnknownSeries_ReturnsNotFound()
    {
        _series.Setup(s => s.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LessonSerie?)null);

        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(
            SeriesId, Participants(adults: 1, youth: 0));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
    }
}
