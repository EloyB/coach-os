using AwesomeAssertions;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Application.Camps.Validators;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class CampEnrollmentPhoneValidatorTests
{
    private readonly SubmitCampEnrollmentRequestValidator _submit = new();
    private readonly CreateManualCampEnrollmentRequestValidator _manual = new();
    private readonly UpdateCampEnrollmentRequestValidator _update = new();

    [Test]
    public void Submit_WithPhone_Passes()
    {
        var request = new SubmitCampEnrollmentRequest
        {
            ParticipantName = "Anna", ParticipantEmail = "anna@test.be", ParticipantPhone = "0470 12 34 56",
        };

        _submit.Validate(request).IsValid.Should().BeTrue();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void Submit_MissingPhone_Fails(string? phone)
    {
        var request = new SubmitCampEnrollmentRequest
        {
            ParticipantName = "Anna", ParticipantEmail = "anna@test.be", ParticipantPhone = phone,
        };

        var result = _submit.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Telefoonnummer is verplicht");
    }

    [TestCase(null)]
    [TestCase("")]
    public void Manual_MissingPhone_Fails(string? phone)
    {
        var request = new CreateManualCampEnrollmentRequest
        {
            ParticipantName = "Anna", ParticipantEmail = "anna@test.be", ParticipantPhone = phone,
        };

        var result = _manual.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Telefoonnummer is verplicht");
    }

    [Test]
    public void Manual_WithPhone_Passes()
    {
        var request = new CreateManualCampEnrollmentRequest
        {
            ParticipantName = "Anna", ParticipantEmail = "anna@test.be", ParticipantPhone = "0470 12 34 56",
        };

        _manual.Validate(request).IsValid.Should().BeTrue();
    }

    [Test]
    public void Update_WithoutPhone_StaysValid()
    {
        var request = new UpdateCampEnrollmentRequest
        {
            ParticipantName = "Anna", ParticipantEmail = "anna@test.be", ParticipantPhone = null,
        };

        _update.Validate(request).IsValid.Should().BeTrue();
    }
}
