using CoachOS.Application.LessonSerie.DTOs;
using CoachOS.Application.LessonSerie.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class WeeklyTemplateEntryRequestValidatorTests
{
    private WeeklyTemplateEntryRequestValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new WeeklyTemplateEntryRequestValidator();
    }

    private static WeeklyTemplateEntryRequest BuildValid() => new()
    {
        DayOfWeek = 0,
        StartTime = "17:00",
        EndTime = "18:00",
        TrainerId = Guid.NewGuid(),
        CourtName = "Baan 1",
        MaxStudents = 4,
    };

    [Test]
    public void Valid_Request_PassesValidation()
    {
        var result = _validator.TestValidate(BuildValid());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestCase(-1)]
    [TestCase(7)]
    public void DayOfWeek_OutOfRange_Fails(int dayOfWeek)
    {
        var request = BuildValid() with { DayOfWeek = dayOfWeek };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.DayOfWeek);
    }

    [TestCase(0)]
    [TestCase(6)]
    public void DayOfWeek_InRange_Passes(int dayOfWeek)
    {
        var request = BuildValid() with { DayOfWeek = dayOfWeek };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.DayOfWeek);
    }

    [TestCase("")]
    [TestCase("7:00")]
    [TestCase("invalid")]
    public void StartTime_InvalidFormat_Fails(string startTime)
    {
        var request = BuildValid() with { StartTime = startTime };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.StartTime);
    }

    [TestCase("")]
    [TestCase("7:00")]
    public void EndTime_InvalidFormat_Fails(string endTime)
    {
        var request = BuildValid() with { EndTime = endTime };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.EndTime);
    }

    [Test]
    public void TrainerId_Null_Passes()
    {
        var request = BuildValid() with { TrainerId = null };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.TrainerId);
    }

    [Test]
    public void CourtName_Null_Passes()
    {
        var request = BuildValid() with { CourtName = null };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.CourtName);
    }

    [Test]
    public void CourtName_TooLong_Fails()
    {
        var request = BuildValid() with { CourtName = new string('X', 101) };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.CourtName);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void MaxStudents_ZeroOrNegative_Fails(int maxStudents)
    {
        var request = BuildValid() with { MaxStudents = maxStudents };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.MaxStudents);
    }
}
