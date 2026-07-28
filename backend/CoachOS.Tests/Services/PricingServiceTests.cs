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

    private void ArrangeOptions(params LessonSeriePrice[] rows)
    {
        _prices.Setup(p => p.GetBySeriesPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows.ToList());
    }

    private static LessonSeriePrice Option(string label, decimal price, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        OrganizationId = OrgId,
        LessonSerieId = SeriesId,
        Label = label,
        TotalPrice = price,
    };

    private static Enrollment Participant(Guid? selectedPriceOptionId = null) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = OrgId,
        LessonSerieId = SeriesId,
        StudentName = "Deelnemer",
        StudentEmail = $"{Guid.NewGuid():N}@test.be",
        SelectedPriceOptionId = selectedPriceOptionId,
    };

    private static List<Enrollment> ParticipantsChoosing(Guid optionId, int count)
    {
        List<Enrollment> list = [];
        for (int i = 0; i < count; i++) list.Add(Participant(optionId));
        return list;
    }

    // ── Gekozen prijsoptie ────────────────────────────────────────────────────

    [Test]
    public async Task Calculate_ChosenOption_ChargesPricePerParticipant()
    {
        ArrangeSeries(legacyPrice: 999m);
        Guid standard = Guid.NewGuid();
        ArrangeOptions(Option("Standaardtarief", 130m, standard));

        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(
            SeriesId, ParticipantsChoosing(standard, count: 3));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(390m); // 130 × 3 deelnemers
        result.Value.UsedLegacyPrice.Should().BeFalse();
        result.Value.Lines.Single().Label.Should().Be("Standaardtarief");
    }

    [Test]
    public async Task Calculate_SingleParticipant_ChargesOptionPriceOnce()
    {
        ArrangeSeries(legacyPrice: 999m);
        Guid youth = Guid.NewGuid();
        ArrangeOptions(Option("Jeugd", 110m, youth));

        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(
            SeriesId, ParticipantsChoosing(youth, count: 1));

        result.Value!.Total.Should().Be(110m);
    }

    [Test]
    public async Task Calculate_ParticipantsChooseDifferentOptions_SumsPerOption()
    {
        ArrangeSeries(legacyPrice: 999m);
        Guid standard = Guid.NewGuid();
        Guid social = Guid.NewGuid();
        ArrangeOptions(Option("Standaardtarief", 130m, standard), Option("Sociaal tarief", 90m, social));

        List<Enrollment> participants =
        [
            Participant(standard),
            Participant(standard),
            Participant(social),
        ];

        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(SeriesId, participants);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(350m); // 2×130 + 1×90
        result.Value.Lines.Should().HaveCount(2);
        result.Value.Lines.Sum(l => l.Amount).Should().Be(result.Value.Total);
    }

    [Test]
    public async Task Calculate_InvalidSelectedOption_ReturnsValidationError()
    {
        ArrangeSeries(legacyPrice: 999m);
        ArrangeOptions(Option("Standaardtarief", 130m));

        List<Enrollment> participants = [Participant(Guid.NewGuid())]; // niet-bestaande optie

        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(SeriesId, participants);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Validation);
    }

    // ── Legacy fallback ─────────────────────────────────────────────────────────

    [Test]
    public async Task Calculate_NoOptions_FallsBackToLegacyPricePerPerson()
    {
        ArrangeSeries(legacyPrice: 120m);
        ArrangeOptions(); // geen opties

        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(
            SeriesId, [Participant(), Participant(), Participant(), Participant()]);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(480m); // 120 × 4
        result.Value.UsedLegacyPrice.Should().BeTrue();
    }

    [Test]
    public async Task Calculate_OptionsExistButNoneSelected_FallsBackToLegacy()
    {
        ArrangeSeries(legacyPrice: 100m);
        ArrangeOptions(Option("Standaardtarief", 130m));

        // Deelnemers zonder gekozen optie (bv. inschrijving van vóór er opties waren).
        Result<PriceBreakdown> result = await _sut.CalculateForGroupAsync(
            SeriesId, [Participant(), Participant()]);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(200m); // 100 × 2
        result.Value.UsedLegacyPrice.Should().BeTrue();
    }

    // ── Randgevallen ──────────────────────────────────────────────────────────

    [Test]
    public async Task Calculate_NoParticipants_ReturnsValidationError()
    {
        ArrangeSeries(legacyPrice: 100m);
        ArrangeOptions();

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
            SeriesId, [Participant()]);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
    }
}
