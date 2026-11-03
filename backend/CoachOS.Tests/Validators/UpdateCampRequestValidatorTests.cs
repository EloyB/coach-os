using CoachOS.Application.Camps.DTOs;
using CoachOS.Application.Camps.Validators;
using FluentAssertions;
using FluentValidation.Results;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class UpdateCampRequestValidatorTests
{
    private readonly UpdateCampRequestValidator _validator = new();

    private static UpdateCampRequest WithShifts(params CreateCampDayTrainerRequest[] shifts) => new(
        Name: "Paaskamp",
        Description: null,
        TennisClubId: Guid.NewGuid(),
        Level: null,
        DayPrice: 120m,
        HalfDayPrice: 60m,
        AllowHalfDays: true,
        StartDate: "2026-04-14",
        EndDate: "2026-04-14",
        RegistrationDeadline: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
        MaxParticipants: 20,
        IsActive: true,
        Days: new List<CreateCampDayRequest>
        {
            new(null, "2026-04-14", "09:00", "12:00", "13:00", "16:00", shifts.ToList()),
        });

    [Test]
    public void Validate_SameTrainerTwoSeparateShifts_Passes()
    {
        Guid trainer = Guid.NewGuid();

        ValidationResult result = _validator.Validate(WithShifts(
            new(trainer, "09:00", "11:00"),
            new(trainer, "13:00", "15:00")));

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
    public void Validate_MalformedTrainerTime_FailsWithoutThrowing()
    {
        ValidationResult result = null!;
        Action act = () => result = _validator.Validate(WithShifts(new CreateCampDayTrainerRequest(Guid.NewGuid(), "10:xx", "11:00")));

        act.Should().NotThrow();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Ongeldige tijd (HH:mm)");
    }

    [Test]
    public void Validate_DuplicateDayDates_Fails()
    {
        UpdateCampRequest request = WithShifts() with
        {
            Days =
            [
                new(null, "2026-04-14", "09:00", "12:00", null, null, []),
                new(null, "2026-04-14", null, null, "13:00", "16:00", []),
            ],
        };

        ValidationResult result = _validator.Validate(request);

        result.Errors.Should().Contain(e => e.ErrorMessage == "Elke kampdag mag maar één keer voorkomen");
    }
}
