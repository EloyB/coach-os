using CoachOS.Application.Trainers.DTOs;
using CoachOS.Application.Trainers.Validators;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class UpdateTrainerRequestValidatorTests
{
    private UpdateTrainerRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateTrainerRequestValidator();

    private static UpdateTrainerRequest Valid() => new()
    {
        FirstName = "Jan",
        LastName = "Janssen",
        WeeklyCapacityHours = 20,
        Notes = "Werkt alleen woensdag"
    };

    [Test]
    public void Valid_request_passes()
    {
        var result = _validator.Validate(Valid());
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Null_notes_passes()
    {
        var result = _validator.Validate(Valid() with { Notes = null });
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Empty_first_or_last_name_fails()
    {
        var result = _validator.Validate(Valid() with { FirstName = "", LastName = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.PropertyName).Should().Contain(new[]
        {
            nameof(UpdateTrainerRequest.FirstName),
            nameof(UpdateTrainerRequest.LastName)
        });
    }

    [Test]
    public void Capacity_below_one_fails()
    {
        var result = _validator.Validate(Valid() with { WeeklyCapacityHours = 0 });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTrainerRequest.WeeklyCapacityHours));
    }

    [Test]
    public void Capacity_above_eighty_fails()
    {
        var result = _validator.Validate(Valid() with { WeeklyCapacityHours = 81 });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTrainerRequest.WeeklyCapacityHours));
    }

    [Test]
    public void Html_in_name_fails()
    {
        var result = _validator.Validate(Valid() with { FirstName = "<script>" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTrainerRequest.FirstName));
    }

    [Test]
    public void Notes_over_1000_chars_fails()
    {
        var result = _validator.Validate(Valid() with { Notes = new string('a', 1001) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTrainerRequest.Notes));
    }
}
