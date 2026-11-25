using AwesomeAssertions;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Application.Enrollments.Validators;
using CoachOS.Domain.Enums;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class SaveEnrollmentFormRequestValidatorTests
{
    private readonly SaveEnrollmentFormRequestValidator _validator = new();

    private static SaveEnrollmentFormRequest WithScope(int scope) => new()
    {
        Fields = [new SaveFormFieldRequest { Label = "Vraag", Type = (int)FormFieldType.Text, Scope = scope }],
    };

    [TestCase((int)FormFieldScope.PerParticipant)]
    [TestCase((int)FormFieldScope.PerEnrollment)]
    public void Scope_Known_IsValid(int scope)
        => _validator.Validate(WithScope(scope)).IsValid.Should().BeTrue();

    [Test]
    public void Scope_Omitted_DefaultsToPerParticipant()
    {
        SaveFormFieldRequest field = new() { Label = "Vraag", Type = (int)FormFieldType.Text };

        field.Scope.Should().Be((int)FormFieldScope.PerParticipant);
    }

    [TestCase(2)]
    [TestCase(-1)]
    public void Scope_Unknown_IsRejected(int scope)
        => _validator.Validate(WithScope(scope)).Errors.Should().ContainSingle(e => e.ErrorMessage == "Ongeldig veldbereik");
}
