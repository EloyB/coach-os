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
        Price: 120m,
        StartDate: "2026-04-14",
        EndDate: "2026-04-16",
        RegistrationDeadline: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
        MaxParticipants: 20,
        Days: new List<CreateCampDayRequest>
        {
            new("2026-04-14", "09:00", "16:00", new List<CreateCampDayTrainerRequest>
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
        ValidationResult result = _validator.Validate(Valid() with { Price = -1m });
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
                new("2026-04-14", "16:00", "09:00", new List<CreateCampDayTrainerRequest>()),
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
                new("2026-04-14", "09:00", "16:00", new List<CreateCampDayTrainerRequest>
                {
                    new(Guid.NewGuid(), "08:00", "16:00"), // start vóór kampstart
                }),
            },
        };
        ValidationResult result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Trainer-uren moeten binnen de kampuren van die dag vallen");
    }
}
