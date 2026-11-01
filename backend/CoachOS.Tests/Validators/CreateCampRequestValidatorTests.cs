using CoachOS.Application.Camps.DTOs;
using CoachOS.Application.Camps.Validators;
using FluentAssertions;
using FluentValidation.Results;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class CreateCampRequestValidatorTests
{
    private CreateCampRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateCampRequestValidator();

    private static CreateCampRequest Valid() => new(
        Name: "Paaskamp",
        Description: null,
        TennisClubId: Guid.NewGuid(),
        Level: null,
        DayPrice: 120m,
        HalfDayPrice: 60m,
        AllowHalfDays: true,
        StartDate: "2026-04-14",
        EndDate: "2026-04-16",
        RegistrationDeadline: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
        MaxParticipants: 20,
        Days: new List<CreateCampDayRequest>
        {
            new(null, "2026-04-14", "09:00", "12:00", "13:00", "16:00", new List<CreateCampDayTrainerRequest>
            {
                new(Guid.NewGuid(), "09:00", "12:00"),
            }),
        });

    [Test]
    public void Validate_Valid_Passes()
    {
        ValidationResult result = _validator.Validate(Valid());
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_EmptyName_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { Name = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Naam is verplicht");
    }

    [Test]
    public void Validate_EmptyClub_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { TennisClubId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Club is verplicht");
    }

    [Test]
    public void Validate_NegativePrice_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { DayPrice = -1m });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Prijs mag niet negatief zijn");
    }

    [Test]
    public void Validate_EndBeforeStart_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { StartDate = "2026-04-16", EndDate = "2026-04-14" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Einddatum moet op of na de startdatum liggen");
    }

    [Test]
    public void Validate_NoDays_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { Days = new List<CreateCampDayRequest>() });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Een kamp heeft minstens één dag nodig");
    }

    [Test]
    public void Validate_DayEndBeforeStart_Fails()
    {
        CreateCampRequest req = Valid() with
        {
            Days = new List<CreateCampDayRequest>
            {
                new(null, "2026-04-14", "12:00", "09:00", "13:00", "16:00", new List<CreateCampDayTrainerRequest>()),
            },
        };
        ValidationResult result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Eindtijd moet na starttijd zijn");
    }

    [Test]
    public void Validate_TrainerWindowOutsideCampHours_Fails()
    {
        CreateCampRequest req = Valid() with
        {
            Days = new List<CreateCampDayRequest>
            {
                new(null, "2026-04-14", "09:00", "12:00", "13:00", "16:00", new List<CreateCampDayTrainerRequest>
                {
                    new(Guid.NewGuid(), "08:00", "16:00"), // start vóór kampstart
                }),
            },
        };
        ValidationResult result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Trainer-uren moeten binnen de kampuren van die dag vallen");
    }

    private static CreateCampRequest WithShifts(params CreateCampDayTrainerRequest[] shifts) => Valid() with
    {
        Days = new List<CreateCampDayRequest>
        {
            new(null, "2026-04-14", "09:00", "12:00", "13:00", "16:00", shifts.ToList()),
        },
    };

    [Test]
    public void Validate_SameTrainerTwoSeparateShiftsOnOneDay_Passes()
    {
        Guid trainer = Guid.NewGuid();

        ValidationResult result = _validator.Validate(WithShifts(
            new(trainer, "09:00", "11:00"),
            new(trainer, "13:00", "15:00")));

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_SameTrainerBackToBackShifts_Passes()
    {
        Guid trainer = Guid.NewGuid();

        ValidationResult result = _validator.Validate(WithShifts(
            new(trainer, "09:00", "11:00"),
            new(trainer, "11:00", "13:00")));

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_SameTrainerOverlappingShifts_Fails()
    {
        Guid trainer = Guid.NewGuid();

        ValidationResult result = _validator.Validate(WithShifts(
            new(trainer, "09:00", "12:00"),
            new(trainer, "11:00", "14:00")));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Een trainer kan niet twee overlappende shiften op dezelfde dag hebben");
    }

    [Test]
    public void Validate_DifferentTrainersAtTheSameTime_Passes()
    {
        ValidationResult result = _validator.Validate(WithShifts(
            new(Guid.NewGuid(), "09:00", "12:00"),
            new(Guid.NewGuid(), "09:00", "12:00")));

        result.IsValid.Should().BeTrue();
    }

    private static CreateCampRequest WithDay(CreateCampDayRequest day) => Valid() with { Days = [day] };

    [Test]
    public void Validate_DayWithoutAnyPart_Fails()
    {
        ValidationResult result = _validator.Validate(WithDay(new(null, "2026-04-14", null, null, null, null, [])));
        result.Errors.Should().Contain(e => e.ErrorMessage == "Een dag heeft minstens één dagdeel nodig");
    }

    [Test]
    public void Validate_AfternoonStartsBeforeMorningEnds_Fails()
    {
        ValidationResult result = _validator.Validate(
            WithDay(new(null, "2026-04-14", "09:00", "12:00", "11:30", "16:00", [])));
        result.Errors.Should().Contain(e => e.ErrorMessage == "De namiddag moet na de voormiddag beginnen");
    }

    [Test]
    public void Validate_PartWithOnlyOneTime_Fails()
    {
        ValidationResult result = _validator.Validate(
            WithDay(new(null, "2026-04-14", "09:00", null, null, null, [])));
        result.Errors.Should().Contain(e => e.ErrorMessage == "Geef van elk dagdeel een start- en eindtijd");
    }

    [Test]
    public void Validate_SinglePartDay_Passes()
        => _validator.Validate(WithDay(new(null, "2026-04-14", "09:15", "12:00", null, null, []))).IsValid
            .Should().BeTrue();

    [Test]
    public void Validate_HalfDayPriceAboveDayPrice_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { DayPrice = 20m, HalfDayPrice = 25m });
        result.Errors.Should().Contain(e => e.ErrorMessage == "De prijs van een halve dag mag niet hoger zijn dan die van een hele dag");
    }

    [Test]
    public void Validate_HalfDaysAllowedWithoutHalfDayPrice_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { HalfDayPrice = 0m });
        result.Errors.Should().Contain(e => e.ErrorMessage == "Geef een prijs voor een halve dag");
    }

    [Test]
    public void Validate_FreeCampWithHalfDaysAllowed_Passes()
        => _validator.Validate(Valid() with { DayPrice = 0m, HalfDayPrice = 0m }).IsValid.Should().BeTrue();

    [Test]
    public void Validate_NegativeMaxParticipants_Fails()
        => _validator.Validate(Valid() with { MaxParticipants = 0 }).IsValid.Should().BeFalse();
}
