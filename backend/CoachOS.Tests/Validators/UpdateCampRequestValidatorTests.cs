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
        Price: 120m,
        StartDate: "2026-04-14",
        EndDate: "2026-04-14",
        RegistrationDeadline: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
        MaxParticipants: 20,
        IsActive: true,
        Days: new List<CreateCampDayRequest>
        {
            new("2026-04-14", "09:00", "16:00", shifts.ToList()),
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
}
