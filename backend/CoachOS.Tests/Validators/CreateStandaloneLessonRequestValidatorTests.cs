using CoachOS.Application.StandaloneLessons.DTOs;
using CoachOS.Application.StandaloneLessons.Validators;
using FluentAssertions;
using FluentValidation.Results;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class CreateStandaloneLessonRequestValidatorTests
{
    private CreateStandaloneLessonRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateStandaloneLessonRequestValidator();

    private static CreateStandaloneLessonRequest Valid() => new()
    {
        Date = "2026-12-01",
        StartTime = "10:00",
        DurationMinutes = 60,
        CourtName = "Baan 1",
        TennisClubId = Guid.NewGuid(),
        Level = 1,
        TrainerId = Guid.NewGuid(),
        MaxParticipants = 4,
        Notes = "Korte notitie",
        ParticipantEmails = new List<string> { "alice@test.com" }
    };

    [Test]
    public void Valid_Request_Passes()
    {
        ValidationResult result = _validator.Validate(Valid());
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Date_Empty_Fails()
    {
        CreateStandaloneLessonRequest req = Valid() with { Date = "" };
        _validator.Validate(req).IsValid.Should().BeFalse();
    }

    [Test]
    public void Date_WrongFormat_Fails()
    {
        CreateStandaloneLessonRequest req = Valid() with { Date = "01-12-2026" };
        _validator.Validate(req).IsValid.Should().BeFalse();
    }

    [TestCase(15)]
    [TestCase(31)]
    [TestCase(150)]
    public void Duration_NotInAllowedSet_Fails(int duration)
    {
        CreateStandaloneLessonRequest req = Valid() with { DurationMinutes = duration };
        _validator.Validate(req).IsValid.Should().BeFalse();
    }

    [TestCase(30)]
    [TestCase(45)]
    [TestCase(60)]
    [TestCase(90)]
    [TestCase(120)]
    public void Duration_InAllowedSet_Passes(int duration)
    {
        CreateStandaloneLessonRequest req = Valid() with { DurationMinutes = duration };
        _validator.Validate(req).IsValid.Should().BeTrue();
    }

    [Test]
    public void CourtName_Empty_Fails()
    {
        CreateStandaloneLessonRequest req = Valid() with { CourtName = "" };
        _validator.Validate(req).IsValid.Should().BeFalse();
    }

    [Test]
    public void Level_OutOfRange_Fails()
    {
        CreateStandaloneLessonRequest req = Valid() with { Level = 5 };
        _validator.Validate(req).IsValid.Should().BeFalse();
    }

    [Test]
    public void Level_Null_Passes()
    {
        CreateStandaloneLessonRequest req = Valid() with { Level = null };
        _validator.Validate(req).IsValid.Should().BeTrue();
    }

    [Test]
    public void TennisClubId_Empty_Fails()
    {
        CreateStandaloneLessonRequest req = Valid() with { TennisClubId = Guid.Empty };
        _validator.Validate(req).IsValid.Should().BeFalse();
    }

    [Test]
    public void TrainerId_Empty_Fails()
    {
        CreateStandaloneLessonRequest req = Valid() with { TrainerId = Guid.Empty };
        _validator.Validate(req).IsValid.Should().BeFalse();
    }

    [Test]
    public void MaxParticipants_Negative_Fails()
    {
        CreateStandaloneLessonRequest req = Valid() with { MaxParticipants = -1 };
        _validator.Validate(req).IsValid.Should().BeFalse();
    }

    [Test]
    public void Emails_Empty_Fails()
    {
        CreateStandaloneLessonRequest req = Valid() with { ParticipantEmails = new List<string>() };
        _validator.Validate(req).IsValid.Should().BeFalse();
    }

    [Test]
    public void Email_Invalid_Fails()
    {
        CreateStandaloneLessonRequest req = Valid() with
        {
            ParticipantEmails = new List<string> { "not-an-email" }
        };
        _validator.Validate(req).IsValid.Should().BeFalse();
    }
}
