using CoachOS.Application.TrainerAvailabilities.DTOs;
using CoachOS.Application.TrainerAvailabilities.Validators;
using AwesomeAssertions;
using FluentValidation.Results;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class SaveOwnTrainerAvailabilityRequestValidatorTests
{
    private readonly SaveOwnTrainerAvailabilityRequestValidator _validator = new();

    private static SaveOwnTrainerAvailabilityRequest Valid() => new(Guid.NewGuid(), 0, "17:00", "21:00");

    [Test]
    public void Validate_Valid_Passes()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Test]
    public void Validate_NoClub_Passes()
        => _validator.Validate(Valid() with { TennisClubId = null }).IsValid.Should().BeTrue();

    [Test]
    public void Validate_EmptyClubGuid_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { TennisClubId = Guid.Empty });
        result.Errors.Should().Contain(e => e.ErrorMessage == "Ongeldige club");
    }

    [Test]
    public void Validate_DayOfWeekOutOfRange_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { DayOfWeek = 7 });
        result.Errors.Should().Contain(e => e.ErrorMessage == "Ongeldige weekdag");
    }

    [Test]
    public void Validate_MalformedStartTime_Fails()
    {
        ValidationResult result = _validator.Validate(Valid() with { StartTime = "25:00" });
        result.Errors.Should().Contain(e => e.ErrorMessage == "Ongeldige starttijd (HH:mm)");
    }

    [TestCase("17:00", "17:00")]
    [TestCase("17:00", "16:00")]
    public void Validate_EndNotAfterStart_Fails(string start, string end)
    {
        ValidationResult result = _validator.Validate(Valid() with { StartTime = start, EndTime = end });
        result.Errors.Should().Contain(e => e.ErrorMessage == "Eindtijd moet na starttijd zijn");
    }
}
