using CoachOS.Application.TrainerAvailabilities.DTOs;
using CoachOS.Application.TrainerAvailabilities.Validators;
using FluentAssertions;
using FluentValidation.Results;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class CreateTrainerAvailabilityRequestValidatorTests
{
    private CreateTrainerAvailabilityRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateTrainerAvailabilityRequestValidator();

    private static CreateTrainerAvailabilityRequest Valid() =>
        new(Guid.NewGuid(), Guid.NewGuid(), 0, "17:00", "21:00");

    [Test]
    public void Validate_ValidRequest_Passes()
    {
        ValidationResult result = _validator.Validate(Valid());
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_EmptyTrainerId_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { TrainerId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Trainer is verplicht");
    }

    [Test]
    public void Validate_EmptyTennisClubId_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { TennisClubId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Club is verplicht");
    }

    [TestCase(-1)]
    [TestCase(7)]
    public void Validate_DayOfWeekOutOfRange_Fails(int day)
    {
        ValidationResult result = _validator.Validate(Valid() with { DayOfWeek = day });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Ongeldige weekdag");
    }

    [TestCase("25:00")]
    [TestCase("9:00")]
    [TestCase("")]
    [TestCase("abc")]
    public void Validate_InvalidStartTime_Fails(string startTime)
    {
        ValidationResult result = _validator.Validate(Valid() with { StartTime = startTime });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Ongeldige starttijd (HH:mm)");
    }

    [Test]
    public void Validate_EndTimeBeforeStartTime_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { StartTime = "21:00", EndTime = "17:00" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Eindtijd moet na starttijd zijn");
    }

    [Test]
    public void Validate_EndTimeEqualsStartTime_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { StartTime = "17:00", EndTime = "17:00" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Eindtijd moet na starttijd zijn");
    }
}
