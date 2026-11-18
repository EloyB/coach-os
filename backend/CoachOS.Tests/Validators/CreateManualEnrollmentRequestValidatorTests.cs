using AwesomeAssertions;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Application.Enrollments.Validators;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class CreateManualEnrollmentRequestValidatorTests
{
    private readonly CreateManualEnrollmentRequestValidator _validator = new();
    private readonly UpdateBasicEnrollmentRequestValidator _updateValidator = new(TimeProvider.System);

    private static CreateManualEnrollmentRequest Valid() => new()
    {
        StudentName = "Anna",
        ContactEmail = "anna@test.be",
        StudentPhone = "0470 12 34 56",
        DateOfBirth = "1990-05-12",
    };

    [Test]
    public void Validate_WithPhone_Passes() =>
        _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void Validate_MissingPhone_Fails(string? phone)
    {
        var result = _validator.Validate(Valid() with { StudentPhone = phone });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Telefoonnummer is verplicht");
    }

    [Test]
    public void UpdateBasic_WithoutPhone_StaysValid()
    {
        var request = new UpdateBasicEnrollmentRequest
        {
            StudentName = "Anna",
            ContactEmail = "anna@test.be",
            StudentPhone = null,
            DateOfBirth = "1990-05-12",
        };

        _updateValidator.Validate(request).IsValid.Should().BeTrue();
    }
}
