using CoachOS.Application.Trainers.DTOs;
using CoachOS.Application.Trainers.Validators;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class SetHeadTrainerClubsRequestValidatorTests
{
    private SetHeadTrainerClubsRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new SetHeadTrainerClubsRequestValidator();

    [Test]
    public void Null_club_ids_fails_without_throwing()
    {
        // Regressietest: de duplicate-check mag NIET draaien bij een null-collectie,
        // anders gooit Enumerable.Distinct(null) een ArgumentNullException (→ HTTP 500).
        SetHeadTrainerClubsRequest request = new() { ClubIds = null! };

        FluentValidation.Results.ValidationResult result = null!;
        Action act = () => result = _validator.Validate(request);

        act.Should().NotThrow();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SetHeadTrainerClubsRequest.ClubIds));
    }

    [Test]
    public void Duplicate_club_ids_fails()
    {
        Guid clubId = Guid.NewGuid();
        SetHeadTrainerClubsRequest request = new() { ClubIds = [clubId, clubId] };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SetHeadTrainerClubsRequest.ClubIds));
    }

    [Test]
    public void Distinct_club_ids_passes()
    {
        SetHeadTrainerClubsRequest request = new() { ClubIds = [Guid.NewGuid(), Guid.NewGuid()] };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Empty_club_ids_passes()
    {
        SetHeadTrainerClubsRequest request = new() { ClubIds = [] };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
