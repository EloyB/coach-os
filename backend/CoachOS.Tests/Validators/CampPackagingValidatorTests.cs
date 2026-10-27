using CoachOS.Application.Camps.DTOs;
using CoachOS.Application.Camps.Validators;
using FluentAssertions;
using FluentValidation.Results;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class CampPackagingValidatorTests
{
    private CreateCampRequestValidator _create = null!;
    private UpdateCampRequestValidator _update = null!;
    private SaveCampPackageRequestValidator _package = null!;

    [SetUp]
    public void SetUp()
    {
        _create = new CreateCampRequestValidator();
        _update = new UpdateCampRequestValidator();
        _package = new SaveCampPackageRequestValidator();
    }

    private static List<CreateCampDayPartRequest> MorningAndAfternoon() => new()
    {
        new("Ochtend", "09:00", "12:00"),
        new("Namiddag", "13:00", "16:00"),
    };

    private static CreateCampDayRequest Day(string date, List<CreateCampDayPartRequest>? parts)
        => new(date, "09:00", "16:00", new List<CreateCampDayTrainerRequest>(), parts);

    private static CampPackageSlotDto Slot(string date = "2026-04-14", int order = 0) => new(date, order);

    private static SaveCampPackageRequest Package(string name = "Ochtenden", decimal price = 60m, params CampPackageSlotDto[] slots)
        => new(null, name, null, price, slots.Length > 0 ? slots.ToList() : new List<CampPackageSlotDto> { Slot() });

    private static CreateCampRequest Camp(
        List<CreateCampDayRequest> days, List<SaveCampPackageRequest>? packages = null) => new(
        Name: "Zomerkamp", Description: null, TennisClubId: Guid.NewGuid(), Level: null, Price: 120m,
        StartDate: "2026-04-14", EndDate: "2026-04-15",
        RegistrationDeadline: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), MaxParticipants: 12,
        Days: days, Packages: packages);

    private static UpdateCampRequest AsUpdate(CreateCampRequest c) => new(
        c.Name, c.Description, c.TennisClubId, c.Level, c.Price, c.StartDate, c.EndDate, c.RegistrationDeadline,
        c.MaxParticipants, true, c.Days, c.Prices, c.Packages);

    [Test]
    public void Camp_WithPartsAndPackage_Passes()
    {
        CreateCampRequest req = Camp(new() { Day("2026-04-14", MorningAndAfternoon()) }, new() { Package() });

        _create.Validate(req).IsValid.Should().BeTrue();
        _update.Validate(AsUpdate(req)).IsValid.Should().BeTrue();
    }

    [Test]
    public void Camp_ThreePartsOnOneDay_Fails()
    {
        List<CreateCampDayPartRequest> parts = MorningAndAfternoon();
        parts.Add(new("Avond", "16:00", "18:00"));

        ValidationResult result = _create.Validate(Camp(new() { Day("2026-04-14", parts) }));

        result.Errors.Should().Contain(e => e.ErrorMessage == "Een dag heeft maximaal 2 dagdelen");
    }

    [Test]
    public void Camp_OverlappingParts_Fails()
    {
        List<CreateCampDayPartRequest> parts = new() { new("Ochtend", "09:00", "12:00"), new("Namiddag", "11:00", "16:00") };

        ValidationResult result = _create.Validate(Camp(new() { Day("2026-04-14", parts) }));

        result.Errors.Should().Contain(e => e.ErrorMessage == "Dagdelen mogen niet overlappen");
    }

    [Test]
    public void Camp_PartEndBeforeStart_Fails()
    {
        List<CreateCampDayPartRequest> parts = new() { new("Ochtend", "12:00", "09:00") };

        ValidationResult result = _create.Validate(Camp(new() { Day("2026-04-14", parts) }));

        result.Errors.Should().Contain(e => e.ErrorMessage == "Eindtijd van het dagdeel moet na de starttijd zijn");
    }

    [Test]
    public void Camp_PartOutsideDayHours_Fails()
    {
        List<CreateCampDayPartRequest> parts = new() { new("Ochtend", "08:00", "12:00") };

        ValidationResult result = _create.Validate(Camp(new() { Day("2026-04-14", parts) }));

        result.Errors.Should().Contain(e => e.ErrorMessage == "Dagdelen moeten binnen de uren van de dag vallen");
    }

    [Test]
    public void Camp_PartWithoutName_Fails()
    {
        List<CreateCampDayPartRequest> parts = new() { new("", "09:00", "12:00") };

        ValidationResult result = _create.Validate(Camp(new() { Day("2026-04-14", parts) }));

        result.Errors.Should().Contain(e => e.ErrorMessage == "Naam van het dagdeel is verplicht");
    }

    [Test]
    public void Camp_PackagesButDayWithoutParts_Fails()
    {
        CreateCampRequest req = Camp(
            new() { Day("2026-04-14", MorningAndAfternoon()), Day("2026-04-15", null) }, new() { Package() });

        _create.Validate(req).Errors.Should().Contain(
            e => e.ErrorMessage == "Elke dag heeft minstens één dagdeel nodig als het kamp pakketten heeft");
        _update.Validate(AsUpdate(req)).Errors.Should().Contain(
            e => e.ErrorMessage == "Elke dag heeft minstens één dagdeel nodig als het kamp pakketten heeft");
    }

    [Test]
    public void Camp_NoPackagesAndDayWithoutParts_Passes()
        => _create.Validate(Camp(new() { Day("2026-04-14", null) })).IsValid.Should().BeTrue();

    [Test]
    public void Camp_MoreThanMaxPackages_Fails()
    {
        List<SaveCampPackageRequest> packages = Enumerable.Range(0, SaveCampPackageRequestValidator.MaxPackages + 1)
            .Select(i => Package($"Pakket {i}")).ToList();

        ValidationResult result = _create.Validate(Camp(new() { Day("2026-04-14", MorningAndAfternoon()) }, packages));

        result.Errors.Should().Contain(
            e => e.ErrorMessage == $"Een kamp heeft maximaal {SaveCampPackageRequestValidator.MaxPackages} pakketten");
    }

    [Test]
    public void Package_Valid_Passes()
        => _package.Validate(Package()).IsValid.Should().BeTrue();

    [Test]
    public void Package_EmptyName_Fails()
        => _package.Validate(Package(name: "")).Errors.Should().Contain(e => e.ErrorMessage == "Naam van het pakket is verplicht");

    [Test]
    public void Package_NegativePrice_Fails()
        => _package.Validate(Package(price: -1m)).Errors.Should().Contain(
            e => e.ErrorMessage == "Prijs van het pakket mag niet negatief zijn");

    [Test]
    public void Package_NoSlots_Fails()
    {
        SaveCampPackageRequest req = Package() with { Slots = new List<CampPackageSlotDto>() };

        _package.Validate(req).Errors.Should().Contain(e => e.ErrorMessage == "Een pakket heeft minstens één dagdeel nodig");
    }

    [Test]
    public void Package_DuplicateSlot_Fails()
        => _package.Validate(Package(slots: new[] { Slot(), Slot() })).Errors.Should().Contain(
            e => e.ErrorMessage == "Een pakket bevat hetzelfde dagdeel meerdere keren");

    [Test]
    public void Package_InvalidSlotDate_Fails()
        => _package.Validate(Package(slots: Slot("14/04/2026"))).Errors.Should().Contain(
            e => e.ErrorMessage == "Ongeldige datum (yyyy-MM-dd)");
}
