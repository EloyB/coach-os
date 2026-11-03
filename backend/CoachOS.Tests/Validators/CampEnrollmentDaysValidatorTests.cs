using CoachOS.Application.Camps.DTOs;
using CoachOS.Application.Camps.Validators;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class CampEnrollmentDaysValidatorTests
{
    [Test]
    public void Manual_WithoutDays_Fails()
        => new CreateManualCampEnrollmentRequestValidator()
            .Validate(new CreateManualCampEnrollmentRequest { ParticipantName = "Emma", ParticipantEmail = "e@x.be" })
            .Errors.Should().Contain(e => e.ErrorMessage == "Kies minstens één dag");

    [Test]
    public void Update_WithoutDays_Fails()
        => new UpdateCampEnrollmentRequestValidator()
            .Validate(new UpdateCampEnrollmentRequest { ParticipantName = "Emma", ParticipantEmail = "e@x.be" })
            .Errors.Should().Contain(e => e.ErrorMessage == "Kies minstens één dag");

    [Test]
    public void Manual_WithDays_Passes()
        => new CreateManualCampEnrollmentRequestValidator()
            .Validate(new CreateManualCampEnrollmentRequest
            {
                ParticipantName = "Emma", ParticipantEmail = "e@x.be",
                Days = [new CampDaySelectionDto(Guid.NewGuid(), true, true)],
            }).IsValid.Should().BeTrue();
}
