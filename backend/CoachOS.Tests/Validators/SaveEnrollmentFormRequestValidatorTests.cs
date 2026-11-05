using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Application.Enrollments.Validators;
using CoachOS.Domain.Enums;
using FluentAssertions;
using FluentValidation.Results;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class SaveEnrollmentFormRequestValidatorTests
{
    private SaveEnrollmentFormRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new SaveEnrollmentFormRequestValidator();

    private static SaveFormFieldRequest AgeField(List<string>? options) => new()
    {
        Label = "Leeftijdscategorie",
        Type = (int)FormFieldType.AgeCategory,
        Order = 0,
        Options = options,
    };

    [Test]
    public void Validate_SingleAgeFieldWithTwoOptions_Passes()
    {
        var request = new SaveEnrollmentFormRequest
        {
            Fields = [AgeField(["4-6", "7-9"])],
        };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_AgeFieldWithOneOption_Fails()
    {
        var request = new SaveEnrollmentFormRequest
        {
            Fields = [AgeField(["4-6"])],
        };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_AgeFieldWithNoOptions_Fails()
    {
        var request = new SaveEnrollmentFormRequest
        {
            Fields = [AgeField(null)],
        };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_TwoAgeFields_Fails()
    {
        var request = new SaveEnrollmentFormRequest
        {
            Fields = [AgeField(["4-6", "7-9"]), AgeField(["10-12", "13-15"])],
        };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_FormWithoutAgeField_Passes()
    {
        var request = new SaveEnrollmentFormRequest
        {
            Fields =
            [
                new() { Label = "Naam ouder", Type = (int)FormFieldType.Text, Order = 0 },
            ],
        };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
